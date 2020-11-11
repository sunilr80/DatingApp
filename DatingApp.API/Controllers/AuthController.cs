﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly IAuthRepository _repository;
		private readonly IConfiguration _configuration;

		public AuthController(IAuthRepository repository,IConfiguration configuration)
		{
			_repository = repository;
			_configuration = configuration;
		}

		[HttpPost("register")]

		public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
		{
			//validate request

			userForRegisterDto.Username = userForRegisterDto.Username.ToLower();
			if (await _repository.UserExists(userForRegisterDto.Username))
				return BadRequest("Username already exists");

			var userToCreate = new User
			{
				Username = userForRegisterDto.Username
			};
			var createdUser = await _repository.Register(userToCreate, userForRegisterDto.Password);
			return StatusCode(201);
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
		{
			var userFromRepo = await _repository.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);

			if (userFromRepo == null)
				return Unauthorized();

			var claims = new[]
			{
				new Claim(ClaimTypes.NameIdentifier,userFromRepo.Id.ToString()),
				new Claim(ClaimTypes.Name,userFromRepo.Username)
			};

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("Appsettings:Token").Value));

			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.Now.AddDays(1),
				SigningCredentials = creds
			};

			var tokenHandler = new JwtSecurityTokenHandler();
			var token = tokenHandler.CreateToken(tokenDescriptor);

			return Ok(new
			{
				token = tokenHandler.WriteToken(token)
			});

		}
	}
}
