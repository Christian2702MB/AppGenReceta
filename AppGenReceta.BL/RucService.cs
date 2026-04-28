using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AppGenReceta.BE;

namespace AppGenReceta.BL
{
    public class RucService
    {
        // Se utiliza static para HttpClient en .NET Framework 4.8 para evitar Socket Exhaustion
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string apiUrl = "https://api.peruapi.com/v1/ruc/";
        
        // Token real proporcionado
        private readonly string apiToken = "8d5fff81c0ad98c254ec3ecdab9a9dbc"; 

        public RucService()
        {
            // Forzar TLS 1.2 y 1.3 para máxima compatibilidad con servidores modernos
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | (SecurityProtocolType)3072 /*Tls12*/ | (SecurityProtocolType)12288 /*Tls13*/;

            // PeruAPI requiere explícitamente "X-API-KEY" en lugar de "Bearer"
            if (!_httpClient.DefaultRequestHeaders.Contains("X-API-KEY"))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-KEY", apiToken);
            }

            // Añadir User-Agent para evitar bloqueos de Cloudflare
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            }
        }

        public async Task<PeruApi_Response> ConsultarRucAsync(string ruc)
        {
            var response = new PeruApi_Response();
            try
            {
                var result = await _httpClient.GetAsync(apiUrl + ruc);
                
                if (result.IsSuccessStatusCode)
                {
                    var json = await result.Content.ReadAsStringAsync();
                    
                    // Regex para extraer razon_social y evitar requerir librerías extra (Newtonsoft.Json) a nivel BL
                    var match = Regex.Match(json, @"""razon_social""\s*:\s*""([^""]+)""");
                    
                    response.Success = true;
                    response.RazonSocial = match.Success ? match.Groups[1].Value : string.Empty;
                }
                else if ((int)result.StatusCode == 429)
                {
                    response.Success = false;
                    response.Message = "Límite de consultas a la API excedido por hoy.";
                }
                else if ((int)result.StatusCode == 404)
                {
                    response.Success = false;
                    response.Message = "RUC no encontrado, inactivo o inválido en sunat.";
                }
                else
                {
                    response.Success = false;
                    response.Message = "Error en proveedor de API. Código HTTP: " + result.StatusCode;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error interno del sistema al consumir la API: " + ex.Message;
            }
            
            return response;
        }
    }
}
