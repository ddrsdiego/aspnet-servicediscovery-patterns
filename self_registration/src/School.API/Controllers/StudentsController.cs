using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using School.API.Filter;
using School.API.Infrastructure;
using School.API.Models;
using System.Linq;
using System.Threading.Tasks;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly DataStore _dataStore;

        public StudentsController(DataStore dataStore)
        {
            _dataStore = dataStore;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            if (_dataStore.Students != null)
            {
                var res = await Task.FromResult(_dataStore.Students).ConfigureAwait(false);

                await Task.Delay(10000).ConfigureAwait(false);

                return Ok(res);
            }

            return NotFound();
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var student = _dataStore.Students.SingleOrDefault(c => c.ID == id);
            if (student != null)
            {
                return Ok(student);
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateModel]
        public IActionResult Post([FromBody]Student student)
        {
            _dataStore.Students.Add(student);
            return Created(Request.GetDisplayUrl() + "/" + student.ID, student);
        }

        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody]Student student)
        {
            var exisitingStudent = _dataStore.Students.SingleOrDefault(c => c.ID == id);

            if (exisitingStudent == null) return NotFound();

            _dataStore.Students.Remove(exisitingStudent);
            _dataStore.Students.Add(student);

            return Ok();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var exisitingStudent = _dataStore.Students.SingleOrDefault(c => c.ID == id);

            if (exisitingStudent == null) return NotFound();

            _dataStore.Students.Remove(exisitingStudent);
            return Ok();
        }
    }
}