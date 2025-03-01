using Bunit;
using LinkBlog.Abstractions;
using LinkBlog.Data;
using LinkBlog.Web.Components.Pages.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LinkBlog.Web.Tests;

public class FormValidationTests : TestContext
{
    private readonly Mock<IPostStore> _mockPostStore;
    private readonly Mock<ILogger<AdminHome>> _mockLogger;

    public FormValidationTests()
    {
        _mockPostStore = new Mock<IPostStore>();
        _mockLogger = new Mock<ILogger<AdminHome>>();

        Services.AddSingleton(_mockPostStore.Object);
        Services.AddSingleton(_mockLogger.Object);
        
        // Setup authorization (since the component has [Authorize] attribute)
        Services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.RequireAssertion(_ => true));
        });
    }

    [Fact]
    public void ValidationErrors_DisplayedForMissingRequiredFields()
    {
        // Arrange
        var component = RenderComponent<AdminHome>();
        var form = component.Find("form");

        // Act - Submit the form without filling in required fields
        form.Submit();

        // Assert - Check for validation messages
        var validationMessages = component.FindAll(".validation-message");
        Assert.NotEmpty(validationMessages);
        
        // Verify specific validation messages
        var messages = component.Markup;
        Assert.Contains("Title is required", messages);
        Assert.Contains("Short title is required", messages);
        Assert.Contains("Content is required", messages);
    }

    [Fact]
    public void ValidationErrors_DisplayedForInvalidShortTitle()
    {
        // Arrange
        var component = RenderComponent<AdminHome>();
        
        // Find inputs
        var titleInput = component.Find("#PostTitle");
        var shortTitleInput = component.Find("#ShortTitle");
        var contentsInput = component.Find("#PostContent");

        // Act - Fill in with valid title and content but invalid short title
        titleInput.Change("Test Title");
        shortTitleInput.Change("Invalid Short Title With Spaces");
        contentsInput.Change("Test content");
        
        // Submit the form
        component.Find("form").Submit();

        // Assert - Check for validation message about short title format
        var validationMessages = component.FindAll(".validation-message");
        Assert.NotEmpty(validationMessages);
        
        Assert.Contains("Short title must contain only lowercase letters, numbers, and hyphens", component.Markup);
    }

    [Fact]
    public void SubmitForm_AcceptsNonValidatedUrl()
    {
        // Arrange
        _mockPostStore
            .Setup(store => store.CreatePostAsync(It.IsAny<Post>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true));
            
        var component = RenderComponent<AdminHome>();
        
        // Fill in required fields
        component.Find("#PostTitle").Change("Test Title");
        component.Find("#ShortTitle").Change("test-title");
        component.Find("#PostContent").Change("Test content");
        component.Find("#PostTags").Change("test-tag");
        
        // Add non-URL text - should still work since we're not validating URLs
        component.Find("#PostLink").Change("not-a-valid-url");
        
        // Act - Submit the form
        component.Find("form").Submit();

        // Verify the creation was attempted
        _mockPostStore.Verify(
            store => store.CreatePostAsync(It.IsAny<Post>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void CreatePost_HandlesUniqueViolationException()
    {
        // Arrange
        _mockPostStore
            .Setup(store => store.CreatePostAsync(It.IsAny<Post>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .Throws(new DbUpdateException("Duplicate key", new Exception("duplicate key value violates unique constraint")));

        var component = RenderComponent<AdminHome>();
        
        // Fill in all required fields
        component.Find("#PostTitle").Change("Test Title");
        component.Find("#ShortTitle").Change("test-title");
        component.Find("#PostContent").Change("Test content");
        component.Find("#PostTags").Change("test-tag");
        
        // Act - Submit the form
        component.Find("form").Submit();
        
        // Verify the PostStore was called
        _mockPostStore.Verify(
            store => store.CreatePostAsync(It.IsAny<Post>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void CreatePost_SuccessfulSubmission()
    {
        // Arrange
        _mockPostStore
            .Setup(store => store.CreatePostAsync(It.IsAny<Post>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true));

        var component = RenderComponent<AdminHome>();
        
        // Fill in all required fields
        component.Find("#PostTitle").Change("Test Title");
        component.Find("#ShortTitle").Change("test-title");
        component.Find("#PostContent").Change("Test content");
        component.Find("#PostTags").Change("test-tag");
        
        // Act - Submit the form
        component.Find("form").Submit();
        
        // Verify the CreatePostAsync method was called
        _mockPostStore.Verify(
            store => store.CreatePostAsync(It.IsAny<Post>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}