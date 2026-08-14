using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;
using Kilavuz.Web.Areas.Panel.Controllers;
using Kilavuz.Web.Application.Interfaces;
using Kilavuz.Web.Domain.Entities;
using Kilavuz.Web.Application;
using Kilavuz.Web.Data;

namespace Kilavuz.Tests;

public class PageControllerTests
{
    [Fact]
    public async Task DeleteAttachment_AsPageCreator_ShouldSucceed_EvenIfNotApplicationOwner()
    {
        // Arrange
        int testUserId = 6; // Yetkili
        int testPageId = 1;
        int testCategoryId = 33;
        int testAttachmentId = 5;

        // Mock Services
        var mockPageService = new Mock<IPageService>();
        var mockAttachmentService = new Mock<IGenericService<PageAttachment>>();
        var mockAttachmentRepo = new Mock<IGenericRepository<PageAttachment>>();
        var mockAppService = new Mock<IGenericService<Application>>();
        var mockCatService = new Mock<IGenericService<Category>>();
        var mockFileService = new Mock<IFileStorageService>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        var mockReorder = new Mock<IReorderService<Page>>();
        var mockDb = new Mock<IDbConnectionFactory>();

        mockEnv.Setup(e => e.ContentRootPath).Returns("C:\\MockPath");

        // Setup Attachment
        var attachment = new PageAttachment 
        { 
            Id = testAttachmentId, 
            PageId = testPageId, 
            StoredFileName = "test.pdf" 
        };
        mockAttachmentService.Setup(s => s.GetByIdAsync(testAttachmentId))
            .ReturnsAsync(new ServiceResult<PageAttachment> { IsSuccess = true, Data = attachment });

        // Setup Page (User IS the creator!)
        var page = new Page 
        { 
            Id = testPageId, 
            CategoryId = testCategoryId, 
            CreatedByUserId = testUserId // <-- Yetkili page creator
        };
        mockPageService.Setup(s => s.GetByIdAsync(testPageId))
            .ReturnsAsync(new ServiceResult<Page> { IsSuccess = true, Data = page });

        // Setup Category (To get ApplicationId)
        mockCatService.Setup(s => s.GetByIdAsync(testCategoryId))
            .ReturnsAsync(new ServiceResult<Category> { IsSuccess = true, Data = new Category { ApplicationId = 20 } });

        // Setup Application (User is NOT the owner!)
        mockAppService.Setup(s => s.GetByIdAsync(20))
            .ReturnsAsync(new ServiceResult<Application> { IsSuccess = true, Data = new Application { CreatedByUserId = 999 } });

        // Setup Controller
        var controller = new PageController(
            mockPageService.Object, mockCatService.Object, mockAppService.Object,
            mockReorder.Object, mockAttachmentService.Object, mockAttachmentRepo.Object, mockFileService.Object, mockEnv.Object, mockDb.Object);

        // Setup User Identity Context
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, testUserId.ToString()), new Claim(ClaimTypes.Role, "Yetkili") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Setup TempData
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        controller.TempData = tempData;

        // Act
        var result = await controller.DeleteAttachment(testAttachmentId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal(testPageId, redirectResult.RouteValues["id"]);
        
        // C# Logical verification: If they had NO permission, it would set ErrorMessage.
        Assert.Null(controller.TempData["ErrorMessage"]); // No error means success!
    }
}
