using Kolok.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Kolok.Services;

public interface IFlightDbService
{
    Task<IList<FlightDTO>> GetFlights(int id);
    
    Task<ActionResult<int>> AddPassenger(int idPassenger, int idFlight);
}