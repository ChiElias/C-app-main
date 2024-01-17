using System.Security.Cryptography;
using System.Text;
using API.DTOs;
using API.Interfaces;
using AutoMapper;
using Company.ClassLibrary1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController : BaseApiController
{
    private readonly IMapper _mapper1;
    private readonly DataContext _dataContext;
    private readonly ITokenService _tokenService;

    public AccountController(IMapper mapper,DataContext dataContext, ITokenService tokenService)
    {
        _mapper1 = mapper;
        _dataContext = dataContext;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await _dataContext.Users
                            .Include(photo => photo.Photos)
                            .SingleOrDefaultAsync(user =>
                            user.UserName == loginDto.UserName);

        if (user is null) return Unauthorized("invalid username");

        using var hmacSHA256 = new HMACSHA256(user.PasswordSalt!);

        var computedHash = hmacSHA256.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password!.Trim()));
        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != user.PasswordHash?[i]) return Unauthorized("invalid password");
        }
        return new UserDto
        {
            Username = user.UserName,
            Token = _tokenService.CreateToken(user),
            PhotoUrl = user.Photos.FirstOrDefault(photo => photo.IsMain)?.Url,
            Aka = user.Aka,
            Gender = user.Gender
        };
    }

    [HttpPost("register")] //ApiController automatically binds the object
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        if (await isUserExists(registerDto.Username!))
            return BadRequest("username is already exists");
        var user = _mapper1.Map<AppUser>(registerDto);
        using var hmacSHA256 = new HMACSHA256();
        user.UserName = registerDto.Username.Trim().ToLower();
        user.PasswordHash = hmacSHA256.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password.Trim()));
        user.PasswordSalt = hmacSHA256.Key;

        _dataContext.Users.Add(user);
        await _dataContext.SaveChangesAsync();

        return new UserDto
        {
            Username = user.UserName,
            Token = _tokenService.CreateToken(user),
            Aka = user.Aka,
            Gender = user.Gender
        };
    }       

    private async Task<bool> isUserExists(string username)
    {
        return await _dataContext.Users.AnyAsync(user => user.UserName == username.ToLower());
    }
}