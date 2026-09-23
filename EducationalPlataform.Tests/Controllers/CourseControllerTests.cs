using EducationalPlataform.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace EducationalPlataform.Tests.Controllers
{
    public class CourseControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public CourseControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetById_ReturnsErrorResponse_WhenCourseNotFound()
        {
            var response = await _client.GetAsync("/api/courses/9999");
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(error);
            Assert.Equal(400, error.StatusCode);
            Assert.Equal("Invalid request data: ", error.Message);
        }
    }
}
