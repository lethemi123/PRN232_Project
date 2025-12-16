using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPRN232.DTO;
using ProjectPRN232.DTO.Auth;
using ProjectPRN232.Models;

namespace ProjectPRN232.Controllers.admin
{
    [Route("api/[controller]")]
    [Authorize (Roles = "Admin")]
    [ApiController]
    public class PeopleController : ControllerBase
    {
        private readonly Prn212AssignmentContext _context;

        public PeopleController(Prn212AssignmentContext context)
        {
            _context = context;
        }


        // GET: api/People
        [HttpGet("getListOfUsers")]
        public async Task<ActionResult<IEnumerable<Person>>> GetPeople()
        {
            var items = await _context.People.Where(u => u.RoleAccount != true).Select(p => new PersonDetail
            {
                UserName = p.UserName,
                FullName = p.Fname + " " + p.Lname,
                email = p.Email,
                phone = p.PhoneNumber,
                Gender = p.Gender,
                DateOfBirth = p.DateOfBirth,
            }).ToListAsync();
            return Ok(items);
        }

        // GET: api/People/5
        [HttpGet("SearchById/{id}")]
        public async Task<ActionResult<Person>> SearchById(int id)
        {
            var items = await _context.People.Where(p => p.PersonId == id).Select(p => new PersonDetail
            {
                UserName = p.UserName,
                FullName = p.Fname + " " + p.Lname,
                email = p.Email,
                phone = p.PhoneNumber,
                Gender = p.Gender,
                DateOfBirth = p.DateOfBirth,
            }).ToListAsync();
            return Ok(items);
           
        }

        [HttpGet("SearchByName/{name}")]
        public async Task<ActionResult<Person>> SearchByName(string name)
        {
            var items = await _context.People.Where(p => (p.Fname + " " + p.Lname).Contains(name)).Select(p => new PersonDetail
            {
                UserName = p.UserName,
                FullName = p.Fname + " " + p.Lname,
                email = p.Email,
                phone = p.PhoneNumber,
                Gender = p.Gender,
                DateOfBirth = p.DateOfBirth,
            }).ToListAsync();
            return Ok(items);
        }


        

        private bool PersonExists(int id)
        {
            return _context.People.Any(e => e.PersonId == id);
        }
    }
}
