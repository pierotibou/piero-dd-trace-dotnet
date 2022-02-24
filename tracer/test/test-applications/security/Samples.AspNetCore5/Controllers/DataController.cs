using Microsoft.AspNetCore.Mvc;
using Samples.AspNetCore5.Models;

namespace Samples.AspNetCore5
{
    [Route("[controller]")]
    public class DataController : Controller
    {
        [HttpPost]
        public IActionResult Index([FromBody]object body)
        {
            return Content("Received\n");
        }

        [Route("model")]
        public IActionResult Model(MyModel model)
        {
            return Content($"Received model with properties: {model}");
        }
    }
}
