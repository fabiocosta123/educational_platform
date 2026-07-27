using AutoMapper;
using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly EducationalPlataformContext _context;
    private readonly IMapper _mapper;

    public UsersController(EducationalPlataformContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public ActionResult<IEnumerable<UserReadDto>> GetAll()
    {
        var users = _context.Users.ToList();
        var usersDto = _mapper.Map<List<UserReadDto>>(users);
        return Ok(usersDto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserReadDto>> GetById(int id)
    {
        var user = await _context.Users
            .Include(u => u.CourseEnrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Teacher)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound($"Usuário com id {id} não encontrado");

        var userDto = _mapper.Map<UserReadDto>(user);
        return Ok(userDto);
    }



    [HttpGet("students")]
    public async Task<ActionResult<IEnumerable<UserReadDto>>> GetStudents()
    {
        var students = await _context.Users
            .Where(u => u.Profile == UserProfile.Student)
            .Include(u => u.CourseEnrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Teacher)
            .ToListAsync();

        var studentsDto = _mapper.Map<List<UserReadDto>>(students);
        return Ok(studentsDto);
    }

    [HttpGet("byName")]
    public async Task<ActionResult<UserReadDto>> GetByName([FromQuery] string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return BadRequest("Nome do usuário é obrigatório.");

        var user = await _context.Users
            .Include(u => u.CourseEnrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Teacher)
            .FirstOrDefaultAsync(u => u.UserName == userName);

        if (user == null)
            return NotFound($"Usuário com nome {userName} não encontrado");

        var userDto = _mapper.Map<UserReadDto>(user);
        return Ok(userDto);
    }



    [HttpPost]
    public ActionResult<UserReadDto> Create([FromBody] UserCreateDto dto)
    {
        var user = _mapper.Map<User>(dto);
        _context.Users.Add(user);
        _context.SaveChanges();

        var userReadDto = _mapper.Map<UserReadDto>(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, userReadDto);
    }

    
    [HttpPost("students")]
    public ActionResult<UserReadDto> CreateStudent([FromBody] StudentCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName))
            return BadRequest("Nome é obrigatório.");

        var student = new User
        {
            UserName = dto.UserName,
            UserEmail = dto.UserEmail,
            CPF = dto.CPF,
            BirthDate = dto.BirthDate,
            Profile = UserProfile.Student,
            Role = "Student"
        };

        _context.Users.Add(student);
        _context.SaveChanges();

        // cria a matrícula do aluno no curso
        var enrollment = new CourseEnrollment
        {
            UserId = student.Id,
            CourseId = dto.CourseId,
            Status = dto.Status,
            ProgressPercentage = 0
        };

        _context.CourseEnrollments.Add(enrollment);
        _context.SaveChanges();

        var studentDto = _mapper.Map<UserReadDto>(student);
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, studentDto);
    }

    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] StudentUpdateDto dto)
    {
        var user = await _context.Users
            .Include(u => u.CourseEnrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Teacher)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound();

        // Atualiza dados básicos
        user.UserName = dto.UserName;
        user.UserEmail = dto.UserEmail;
        user.PhoneNumber = dto.PhoneNumber;
        user.BirthDate = dto.BirthDate;
        user.Profile = UserProfile.Student;

        // 🔹 Atualiza matrículas
        var existingCourseIds = user.CourseEnrollments.Select(e => e.CourseId).ToList();
        var newCourseIds = dto.CourseEnrollments.Select(e => e.CourseId).ToList();

        // Remove cursos que não estão mais na lista
        var toRemove = user.CourseEnrollments.Where(e => !newCourseIds.Contains(e.CourseId)).ToList();
        foreach (var enrollment in toRemove)
            _context.CourseEnrollments.Remove(enrollment);

        // Atualiza ou adiciona cursos
        foreach (var enrollmentDto in dto.CourseEnrollments)
        {
            var enrollment = user.CourseEnrollments.FirstOrDefault(e => e.CourseId == enrollmentDto.CourseId);
            if (enrollment != null)
            {
                enrollment.Status = enrollmentDto.Status;
            }
            else
            {
                user.CourseEnrollments.Add(new CourseEnrollment
                {
                    CourseId = enrollmentDto.CourseId,
                    UserId = user.Id,
                    Status = enrollmentDto.Status,
                    ProgressPercentage = 0
                });
            }
        }

        await _context.SaveChangesAsync();

        //recarrega com includes Teacher e Course 
        var updatedUser = await _context.Users
            .Include(u => u.CourseEnrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Teacher)
            .FirstOrDefaultAsync(u => u.Id == id);

        var userDto = _mapper.Map<UserReadDto>(updatedUser);
        return Ok(userDto);
    }

    
    [HttpPut("pix/pay/{paymentId}")]
    public async Task<IActionResult> MarkAsPaid(int paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null)
            return NotFound("Pagamento não encontrado");

        payment.Status = "PAID";
        payment.PaidAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }

    
    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null)
            throw new ArgumentException($"Course with id {id} not found");

        _context.Users.Remove(user);
        _context.SaveChanges();
        return NoContent();
    }
}
