import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnDestroy,
  Output,
  ViewChild,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import type * as L from 'leaflet';
import { LanguageService } from '../../../core/services/language.service';
import { ThemeService } from '../../../core/services/theme.service';

export interface MapCoords {
  lat: number;
  lng: number;
}

// Reusable store location map.
//
// • Shopper / read-only mode: a keyless OpenStreetMap iframe embed plus a
//   "get directions" link that opens the user's maps app.
// • Merchant / editable mode: a fully interactive Leaflet map. The owner can
//   click anywhere or drag the pin to set the exact store location, or tap
//   "use my location" for a one-shot GPS pick — no typing coordinates.
//
// Inputs are signal-backed so links/labels update live as the parent form
// changes lat/lng, and the interactive pin stays in sync when the owner edits
// the raw latitude/longitude fields by hand.
@Component({
  selector: 'app-store-map',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './store-map.html',
  styleUrl: './store-map.css',
})
export class StoreMapComponent implements AfterViewInit, OnDestroy {
  private sanitizer = inject(DomSanitizer);
  lang = inject(LanguageService);
  private themeSvc = inject(ThemeService);

  constructor() {
    // Swap the basemap to match light / dark whenever the app theme changes.
    effect(() => {
      const dark = this.themeSvc.theme() === 'dark';
      this.applyBasemap(dark);
    });
  }

  private readonly _lat = signal<number | null>(null);
  private readonly _lng = signal<number | null>(null);

  @Input() set lat(value: number | null | undefined) {
    this._lat.set(this.clean(value));
    this.syncPinFromInputs();
  }
  @Input() set lng(value: number | null | undefined) {
    this._lng.set(this.clean(value));
    this.syncPinFromInputs();
  }
  @Input() name = '';
  @Input() address = '';
  @Input() compact = false;
  // editable = merchant mode: shows the interactive picker and emits picks.
  @Input() editable = false;

  @Output() locationChange = new EventEmitter<MapCoords>();

  @ViewChild('pickMap') private pickMapEl?: ElementRef<HTMLElement>;

  readonly locating = signal(false);
  readonly geoError = signal('');

  // Default framing when nothing is set yet: central Cairo, city-level zoom.
  private static readonly FALLBACK: MapCoords = { lat: 30.0444, lng: 31.2357 };

  // Leaflet is loaded on demand (only the interactive merchant picker needs it),
  // so it never weighs down the initial bundle or the read-only shopper view.
  private leaflet?: typeof import('leaflet');
  private map?: L.Map;
  private marker?: L.Marker;
  private tiles?: L.TileLayer;

  readonly hasCoords = computed(() => this._lat() !== null && this._lng() !== null);

  // Whether there is anything to render at all (coords, an address to search, or
  // the editable controls). Lets the parent drop the block entirely otherwise.
  readonly hasContent = computed(() => this.editable || this.hasCoords() || !!this.searchQuery());

  L(en: string, ar: string): string { return this.lang.t(en, ar); }

  ngAfterViewInit(): void {
    if (this.editable) void this.initPicker();
  }

  ngOnDestroy(): void {
    this.map?.remove();
    this.map = undefined;
    this.marker = undefined;
  }

  // ── Interactive picker (merchant mode) ──────────────────────────────────────
  private async initPicker(): Promise<void> {
    const host = this.pickMapEl?.nativeElement;
    if (!host || this.map) return;

    const Lf = await import('leaflet');
    this.leaflet = Lf;
    // The component may have been torn down while the chunk loaded.
    if (!this.pickMapEl?.nativeElement || this.map) return;

    const start = this.hasCoords()
      ? { lat: this._lat()!, lng: this._lng()! }
      : StoreMapComponent.FALLBACK;
    const zoom = this.hasCoords() ? 15 : 12;

    this.map = Lf.map(host, {
      center: [start.lat, start.lng],
      zoom,
      zoomControl: true,
      attributionControl: true,
    });
    this.applyBasemap(this.themeSvc.theme() === 'dark');

    // Click anywhere on the map to drop / move the pin.
    this.map.on('click', (e: L.LeafletMouseEvent) => this.commitPick(e.latlng.lat, e.latlng.lng));

    if (this.hasCoords()) this.placeMarker(start.lat, start.lng);

    // The map is often created right after its tab/container becomes visible, so
    // Leaflet may have measured a zero-size box — recompute once laid out.
    setTimeout(() => this.map?.invalidateSize(), 0);
  }

  // Modern CARTO basemaps (no API key): soft "Voyager" cartography in light mode,
  // "Dark Matter" in dark mode. Swapped live when the theme toggles.
  private applyBasemap(dark: boolean): void {
    if (!this.map || !this.leaflet) return;
    const url = dark
      ? 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png'
      : 'https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png';
    if (this.tiles) this.map.removeLayer(this.tiles);
    this.tiles = this.leaflet.tileLayer(url, {
      subdomains: 'abcd',
      maxZoom: 20,
      attribution: '&copy; OpenStreetMap &copy; CARTO',
    }).addTo(this.map);
  }

  // Build the teardrop pin as an inline-styled SVG so it needs no image assets
  // (Leaflet's default marker images break under bundlers) and no global CSS.
  private pinIcon(): L.DivIcon {
    return this.leaflet!.divIcon({
      className: 'store-pin',
      html:
        '<span class="pin-halo"></span>' +
        '<span class="pin-body">' +
        '<svg width="32" height="42" viewBox="0 0 24 32" xmlns="http://www.w3.org/2000/svg">' +
        '<path d="M12 0C5.7 0 .6 5.1.6 11.4.6 20 12 32 12 32s11.4-12 11.4-20.6C23.4 5.1 18.3 0 12 0z" ' +
        'fill="#bb502b"/>' +
        '<circle cx="12" cy="11.3" r="4.2" fill="#fff"/></svg>' +
        '</span>',
      iconSize: [32, 42],
      iconAnchor: [16, 40],
    });
  }

  // Create the pin on first use, then just move it. Dragging the pin commits a
  // pick the same way a map click does.
  private placeMarker(lat: number, lng: number): void {
    if (!this.map || !this.leaflet) return;
    if (this.marker) {
      this.marker.setLatLng([lat, lng]);
      return;
    }
    this.marker = this.leaflet.marker([lat, lng], { draggable: true, icon: this.pinIcon(), autoPan: true }).addTo(this.map);
    this.marker.on('dragend', () => {
      const p = this.marker!.getLatLng();
      this.commitPick(p.lat, p.lng);
    });
  }

  // A pick from any source (map click, pin drag, GPS): round, store, draw, emit.
  private commitPick(lat: number, lng: number): void {
    const rlat = +lat.toFixed(6);
    const rlng = +lng.toFixed(6);
    this._lat.set(rlat);
    this._lng.set(rlng);
    this.placeMarker(rlat, rlng);
    this.locationChange.emit({ lat: rlat, lng: rlng });
  }

  // Keep the pin aligned when the owner edits the raw lat/lng fields by hand.
  // Guarded against the echo of our own emit (parent writes the value straight
  // back into the [lat]/[lng] inputs) so it never fights the user.
  private syncPinFromInputs(): void {
    if (!this.editable || !this.map) return;
    const lat = this._lat();
    const lng = this._lng();
    if (lat === null || lng === null) return;
    if (this.marker) {
      const p = this.marker.getLatLng();
      if (Math.abs(p.lat - lat) < 1e-6 && Math.abs(p.lng - lng) < 1e-6) return;
    }
    this.placeMarker(lat, lng);
    this.map.setView([lat, lng], Math.max(this.map.getZoom(), 14), { animate: true });
  }

  // OpenStreetMap embed (read-only shopper view): a small bounding box around the
  // point, with a marker.
  readonly embedUrl = computed<SafeResourceUrl | null>(() => {
    if (!this.hasCoords()) return null;
    const lat = this._lat()!;
    const lng = this._lng()!;
    const d = 0.008; // ~street-level framing
    const bbox = [lng - d, lat - d, lng + d, lat + d].map(n => n.toFixed(6)).join('%2C');
    const url =
      `https://www.openstreetmap.org/export/embed.html?bbox=${bbox}` +
      `&layer=mapnik&marker=${lat.toFixed(6)}%2C${lng.toFixed(6)}`;
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  });

  // Universal Google Maps link: turn-by-turn directions to the coordinates, or a
  // name/address search when coordinates are not set yet.
  readonly mapsUrl = computed<string>(() => {
    if (this.hasCoords()) {
      return `https://www.google.com/maps/dir/?api=1&destination=${this._lat()},${this._lng()}`;
    }
    const query = this.searchQuery();
    return query ? `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(query)}` : '';
  });

  // Short "lat, lng" label shown under the map.
  readonly coordsLabel = computed(() =>
    this.hasCoords() ? `${this._lat()!.toFixed(5)}, ${this._lng()!.toFixed(5)}` : '',
  );

  private searchQuery(): string {
    return [this.name, this.address].map(s => s?.trim()).filter(Boolean).join(' ');
  }

  // Fill coordinates from the device GPS / browser location.
  useMyLocation(): void {
    if (!('geolocation' in navigator)) {
      this.geoError.set(this.L('Location is not supported on this device.', 'تحديد الموقع غير مدعوم على هذا الجهاز.'));
      return;
    }
    this.locating.set(true);
    this.geoError.set('');
    navigator.geolocation.getCurrentPosition(
      pos => {
        this.locating.set(false);
        const lat = +pos.coords.latitude.toFixed(6);
        const lng = +pos.coords.longitude.toFixed(6);
        this.commitPick(lat, lng);
        this.map?.setView([lat, lng], 16, { animate: true });
      },
      err => {
        this.locating.set(false);
        this.geoError.set(
          err.code === err.PERMISSION_DENIED
            ? this.L('Location permission was denied.', 'تم رفض إذن الوصول للموقع.')
            : this.L('Could not get your location. Try again.', 'تعذّر تحديد موقعك. حاول مرة أخرى.'),
        );
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 },
    );
  }

  // Reject NaN / out-of-range values so the embed never builds a broken URL.
  private clean(value: number | null | undefined): number | null {
    if (value === null || value === undefined) return null;
    const n = Number(value);
    // Shared by lat (-90..90) and lng (-180..180); use the looser union range
    // so a valid longitude is never rejected, but garbage/NaN still is.
    if (Number.isNaN(n) || n < -180 || n > 180) return null;
    return n;
  }
}
