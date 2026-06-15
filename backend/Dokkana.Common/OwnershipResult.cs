namespace Dokkana.Common
{
    // Outcome of an ownership-guarded mutation so controllers can map to the
    // correct HTTP status: 404 for missing, 403 for "exists but not yours".
    public enum OwnershipResult
    {
        Success,
        NotFound,
        Forbidden
    }
}
