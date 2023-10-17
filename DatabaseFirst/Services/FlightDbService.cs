using System.Data.SqlClient;
using Kolok.DTO;
using Microsoft.AspNetCore.Mvc;

namespace Kolok.Services;

public class FlightDbService : IFlightDbService
{
    private readonly IConfiguration _configuration;

    public FlightDbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<IList<FlightDTO>> GetFlights(int id)
    {
        List<FlightDTO> flights = new List<FlightDTO>();
        
        await using SqlConnection sqlConnection = new(_configuration.GetConnectionString("DefaultConnection"));
        await using SqlCommand sqlCommand = new();
        
        string sql;
        sql = "SELECT cd.City, p.IdPlane, p.Name, p.MaxSeats FROM Flight_Passenger fp " +
              "JOIN Flight f ON fp.IdFlight = f.IdFlight " +
              "JOIN CityDict cd ON f.IdCityDict = cd.IdCityDict " +
              "JOIN Plane p ON f.IdPlane = p.IdPlane " +
              "WHERE fp.IdPassenger = @id";

        sqlCommand.Parameters.AddWithValue("@id", id);
        
        sqlCommand.CommandText = sql;
        sqlCommand.Connection = sqlConnection;
        
        await sqlConnection.OpenAsync();
        await using SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync();
        while (await sqlDataReader.ReadAsync())
        {
            FlightDTO flight = new()
            {
                City = sqlDataReader["City"].ToString(),
                IdPlane = int.Parse(sqlDataReader["IdPlane"].ToString()),
                Name = sqlDataReader["Name"].ToString(),
                MaxSeats = int.Parse(sqlDataReader["MaxSeats"].ToString())
            };
            flights.Add(flight);
        }
        await sqlConnection.CloseAsync();

        return flights;
    }

    public async Task<ActionResult<int>> AddPassenger(int idPassenger, int idFlight)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();
        await using SqlTransaction transaction = connection.BeginTransaction();

        //Getting list of flights which date is bigger then current
        List<int> futureFlightsIds = new List<int>();

        string sql = "SELECT * FROM Flight f WHERE f.FlightDate > " + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
        
        Console.WriteLine(sql);
        
        await using SqlCommand command = new SqlCommand(sql, connection, transaction);

        await using SqlDataReader sqlDataReader = await command.ExecuteReaderAsync();
        while (await sqlDataReader.ReadAsync())
        {
            futureFlightsIds.Add(int.Parse(sqlDataReader["IdFlight"].ToString()));
        }
        
        await sqlDataReader.CloseAsync();

        if (!futureFlightsIds.Contains(idFlight))
        {
            transaction.RollbackAsync();
            return 1; //Flight with this id is done or doesn't exist
        }

        //Checking if MaxSeats is OK
        //First check maxseats
        
        int maxSeats = 0;
        string sql1 = "SELECT p.MaxSeats FROM Flight f JOIN Plane p ON f.IdPlane = p.IdPlane WHERE f.IdFlight = @idFlight";
        
        await using SqlCommand command1 = new SqlCommand(sql1, connection, transaction);
        command1.Parameters.AddWithValue("@idFlight", idFlight);

        await using SqlDataReader sqlDataReader1 = await command1.ExecuteReaderAsync();
        while (await sqlDataReader1.ReadAsync())
        {
            maxSeats = int.Parse(sqlDataReader1["MaxSeats"].ToString());
        }

        await sqlDataReader1.CloseAsync();
        
        //then if passenger count is less
        
        int passengerCount = 0;
        string sql2 = "SELECT COUNT(IdPassenger) Count FROM Flight_Passenger fp WHERE fp.IdFlight = @idFlight";
        
        await using SqlCommand command2 = new SqlCommand(sql2, connection, transaction);
        command2.Parameters.AddWithValue("@idFlight", idFlight);

        await using SqlDataReader sqlDataReader2 = await command2.ExecuteReaderAsync();
        while (await sqlDataReader2.ReadAsync())
        {
            passengerCount = int.Parse(sqlDataReader2["Count"].ToString());
        }

        await sqlDataReader2.CloseAsync();

        if (passengerCount >= maxSeats)
        {
            transaction.RollbackAsync();
            return 2;// There are max of passengers in this flight
        }
        
        //Checking if passenger exists in flight

        string sql3 = "SELECT * FROM Flight_Passenger fp WHERE fp.IdFlight = @idFlight AND fp.IdPassenger = @idPassenger";
        string checkSql = "";
        
        await using SqlCommand command3 = new SqlCommand(sql3, connection, transaction);
        command3.Parameters.AddWithValue("@idFlight", idFlight);
        command3.Parameters.AddWithValue("@idPassenger", idPassenger);

        await using SqlDataReader sqlDataReader3 = await command3.ExecuteReaderAsync();
        while (await sqlDataReader3.ReadAsync())
        {
            checkSql = sqlDataReader3["IdFlight"].ToString();
        }
        
        await sqlDataReader3.CloseAsync();

        if (!string.IsNullOrWhiteSpace(checkSql))
        {
            transaction.RollbackAsync();
            return 3;//Passenger already exists in flight
        }

        string sql4 = "INSERT INTO Flight_Passenger (IdFlight, IdPassenger) VALUES (@idFlight, @idPassenger)";
        await using SqlCommand command4 = new SqlCommand(sql4, connection, transaction);
        command4.Parameters.AddWithValue("@idFlight", idFlight);
        command4.Parameters.AddWithValue("@idPassenger", idPassenger);
        
        await command4.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        return 4; //Success
    }
}