using Kolok.DTO;
using Kolok.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kolok.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightsController : Controller
{
    private readonly IFlightDbService _flightDbService;

    public FlightsController(IFlightDbService flightDbService)
    {
        _flightDbService = flightDbService;
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFlights(int id)
    {
        IList<FlightDTO> flights = await _flightDbService.GetFlights(id);
        if (flights.Count != 0)
        {
            return Ok(flights);
        }
        return BadRequest("There are no passenger with id: " + id);
    }

    [HttpPost("{idPassenger}/{idFlight}")]
    public async Task<IActionResult> AddPassenger(int idPassenger, int idFlight)
    {
        ActionResult<int> res = await _flightDbService.AddPassenger(idPassenger, idFlight);
        int result = res.Value;
        switch (result)
        {
            case 1:
                return NotFound("Flight with this id is done or doesn't exist");
            case 2:
                return BadRequest("There are max of passengers in this flight");
            case 3:
                return BadRequest("Passenger already exists in flight");
            case 4:
                return Ok("Passenger was added");
            default:
                return BadRequest();
        }
    }
}