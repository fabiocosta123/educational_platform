using AutoMapper;
using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


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

        var today = DateTime.Today;

        if (dto.BirthDate > today.AddYears(-10))
            return BadRequest("Aluno deve ter pelo menos 10 anos de idade.");

        if (dto.BirthDate < today.AddYears(-100))
            return BadRequest("Aluno deve ter no máximo 100 anos de idade.");

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
    public async Task<IActionResult> Update(
    int id,
    [FromBody] StudentUpdateDto dto)
    {
        try
        {
            Console.WriteLine("========== UPDATE STUDENT ==========");

            Console.WriteLine($"Authenticated: {User.Identity?.IsAuthenticated}");
            Console.WriteLine($"Name: {User.Identity?.Name}");
            Console.WriteLine($"Role: {User.FindFirst(ClaimTypes.Role)?.Value}");
            Console.WriteLine($"Is Coordinator: {User.IsInRole("Coordinator")}");
            Console.WriteLine($"User ID: {User.FindFirst(ClaimTypes.NameIdentifier)?.Value}");

            Console.WriteLine($"Student ID: {id}");
            Console.WriteLine($"CurrentCourseId: {dto.CurrentCourseId}");
            Console.WriteLine($"NewCourseId: {dto.NewCourseId}");

            var today = DateTime.Today;

            Console.WriteLine("Validando idade...");

            if (dto.BirthDate > today.AddYears(-12))
                return BadRequest("Aluno deve ter pelo menos 12 anos.");

            if (dto.BirthDate < today.AddYears(-100))
                return BadRequest("Aluno deve ter no máximo 100 anos.");

            Console.WriteLine("Idade válida.");

            Console.WriteLine("Buscando aluno no banco...");

            var user = await _context.Users
                .Include(u => u.CourseEnrollments)
                .FirstOrDefaultAsync(u => u.Id == id);

            Console.WriteLine("Consulta do aluno finalizada.");

            if (user == null)
            {
                Console.WriteLine("Aluno não encontrado.");
                return NotFound();
            }

            Console.WriteLine($"Aluno encontrado: {user.UserName}");

            Console.WriteLine(
                $"Quantidade de matrículas: {user.CourseEnrollments.Count}"
            );

            foreach (var enrollmentItem in user.CourseEnrollments)
            {
                Console.WriteLine(
                    $"Matrícula => Id: {enrollmentItem.Id}, " +
                    $"UserId: {enrollmentItem.UserId}, " +
                    $"CourseId: {enrollmentItem.CourseId}, " +
                    $"Status: {enrollmentItem.Status}"
                );
            }

            Console.WriteLine(
                $"Procurando matrícula do curso atual: {dto.CurrentCourseId}"
            );

            /* var enrollment = user.CourseEnrollments
                 .FirstOrDefault(e => e.CourseId == dto.CurrentCourseId);

             if (enrollment == null)
             {
                 Console.WriteLine("MATRÍCULA NÃO ENCONTRADA.");
                 return BadRequest("Matricula não encontrada");
             }

             Console.WriteLine(
                 $"Matrícula encontrada: ID {enrollment.Id}"
             );

             Console.WriteLine(
                 $"Alterando curso: {enrollment.CourseId} -> {dto.NewCourseId}"
             );

             user.UserName = dto.UserName;
             user.UserEmail = dto.UserEmail;
             user.PhoneNumber = dto.PhoneNumber;
             user.BirthDate = dto.BirthDate;

             enrollment.CourseId = dto.NewCourseId;
             enrollment.Status = dto.Status;

             Console.WriteLine("Dados alterados em memória.");

             await _context.SaveChangesAsync();

             Console.WriteLine("SaveChangesAsync executado com sucesso.");

             Console.WriteLine("========== UPDATE FINALIZADO ==========");

             return Ok();*/
            var enrollment = user.CourseEnrollments
     .FirstOrDefault(e => e.CourseId == dto.CurrentCourseId);

            if (enrollment == null)
                return BadRequest("Matrícula não encontrada.");

            if (dto.CurrentCourseId == dto.NewCourseId)
            {
                enrollment.Status = dto.Status;

                await _context.SaveChangesAsync();

                return Ok();
            }

            // Verifica se o aluno já está matriculado no novo curso
            var alreadyEnrolled = await _context.CourseEnrollments
                .AnyAsync(e =>
                    e.UserId == id &&
                    e.CourseId == dto.NewCourseId);

            if (alreadyEnrolled)
                return BadRequest("O aluno já está matriculado neste curso.");

            // Remove a matrícula atual
            _context.CourseEnrollments.Remove(enrollment);

            // Cria a nova matrícula
            var newEnrollment = new CourseEnrollment
            {
                UserId = id,
                CourseId = dto.NewCourseId,
                Status = dto.Status,
                ProgressPercentage = 0,
                CompletedLessons = 0,
                TotalLessons = 0,
                StartDate = DateTime.UtcNow
            };

            _context.CourseEnrollments.Add(newEnrollment);

            await _context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine("========== ERRO NO UPDATE ==========");

            Console.WriteLine($"Tipo: {ex.GetType().FullName}");
            Console.WriteLine($"Mensagem: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                Console.WriteLine("----- INNER EXCEPTION -----");
                Console.WriteLine(ex.InnerException.Message);
            }

            return StatusCode(500, "Erro interno ao atualizar aluno.");
        }
    }

    [HttpPut("pix/pay/{paymentId}")]
    public async Task<IActionResult> MarkAsPaid(int paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null)
            return NotFound("Pagamento não encontrado");

        payment.Status = PaymentStatus.Paid;
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
