using System.Threading;
using System.Threading.Tasks;
using MyWeatherApp.Models;

namespace MyWeatherApp.Services
{
    public interface IResultHandler
    {
        Task HandleResultAsync(WeatherForecast forecast, CancellationToken cancellationToken);
    }
}
