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

        // REST + SOAP full CRUD coverage
        [OperationContract]
        Task<List<Incident>> GetAllIncidents();

        [OperationContract]
        Task<Incident?> GetIncidentById(int id);

        [OperationContract]
        Task<Incident> CreateIncident(Incident incident);

        [OperationContract]
        Task<bool> UpdateIncident(int id, Incident incident);

        [OperationContract]
        Task<bool> DeleteIncident(int id);
    }
}