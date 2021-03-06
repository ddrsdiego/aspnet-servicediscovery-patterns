﻿using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using School.API.Filter;
using School.API.Infrastructure;
using School.API.Models;
using System.Linq;

namespace School.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly DataStore _dataStore;

        public CoursesController(DataStore dataStore)
        {
            _dataStore = dataStore;
        }

        [HttpGet]
        public IActionResult Get()
        {
            if (_dataStore.Courses != null)
                return Ok(_dataStore.Courses);

            return NotFound();
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var course = _dataStore.Courses.SingleOrDefault(c => c.ID == id);
            if (course != null)
            {
                return Ok(course);
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateModel]
        public IActionResult Post([FromBody]Course course)
        {
            _dataStore.Courses.Add(course);
            return Created(Request.GetDisplayUrl() + "/" + course.ID, course);
        }

        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody]Course course)
        {
            var exisitingCourse = _dataStore.Courses.SingleOrDefault(c => c.ID == id);

            if (exisitingCourse == null) return NotFound();

            _dataStore.Courses.Remove(exisitingCourse);
            _dataStore.Courses.Add(course);

            return Ok();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var exisitingCourse = _dataStore.Courses.SingleOrDefault(c => c.ID == id);

            if (exisitingCourse == null) return NotFound();

            _dataStore.Courses.Remove(exisitingCourse);
            return Ok();
        }
    }
}