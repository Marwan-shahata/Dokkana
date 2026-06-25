// Development environment. Points at the local ASP.NET Core API.
// Backend dev URLs (see backend/Dokkana.API/Properties/launchSettings.json):
//   https://localhost:7264  |  http://localhost:5034
// Using http avoids the self-signed-cert prompt during development.
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5034/api',
};
