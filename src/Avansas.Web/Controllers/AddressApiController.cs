using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Avansas.Web.Controllers;

[Route("api/address")]
[ApiController]
public class AddressApiController : ControllerBase
{
    private static List<CityData>? _cities;
    private readonly IWebHostEnvironment _env;

    public AddressApiController(IWebHostEnvironment env)
    {
        _env = env;
    }

    private List<CityData> LoadCities()
    {
        if (_cities != null) return _cities;

        var path = Path.Combine(_env.WebRootPath, "data", "cities.json");
        var json = System.IO.File.ReadAllText(path);
        _cities = JsonSerializer.Deserialize<List<CityData>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<CityData>();
        return _cities;
    }

    [HttpGet("cities")]
    public IActionResult GetCities()
    {
        var cities = LoadCities().Select(c => c.Name).OrderBy(n => n).ToList();
        return Ok(cities);
    }

    [HttpGet("districts")]
    public IActionResult GetDistricts([FromQuery] string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return BadRequest("City parameter is required.");

        var cityData = LoadCities()
            .FirstOrDefault(c => c.Name.Equals(city, StringComparison.OrdinalIgnoreCase));

        if (cityData == null)
            return NotFound("City not found.");

        return Ok(cityData.Districts.OrderBy(d => d));
    }

    private class CityData
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Districts { get; set; } = new();
    }
}
