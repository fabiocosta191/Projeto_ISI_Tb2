using SafeHome.Data.Models;
using System.ServiceModel; // Namespace clássico do SOAP

namespace SafeHome.API.Soap
{
    [ServiceContract] // Define que isto é um serviço SOAP
    public interface IIncidentService
    {
        [OperationContract] // Define que este método está visível na web
        Task<string> ReportIncident(string type, string description, int buildingId, string severity);

        [OperationContract]
        Task<List<Incident>> GetUnresolvedIncidents();
    }
}