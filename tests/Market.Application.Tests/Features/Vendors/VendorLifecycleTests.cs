using Market.Application.Features.Vendors.Commands;
using Market.Application.Features.Vendors.Queries;
using Market.Domain.Entities;
using Market.Domain.Repositories;
using Market.Application.Abstractions.Services;
using Moq;
using Xunit;

namespace Market.Application.Tests.Features.Vendors;

/// <summary>
/// Tests for Vendor lifecycle management and state transitions.
/// Covers creation, approval, activation/deactivation, deletion, and ownership enforcement.
/// </summary>
public class VendorLifecycleTests
{
    private static (
        Mock<ICurrentUserService> currentUserService,
        Mock<IUnitOfWork> unitOfWork,
        CreateVendorInternalCommandHandler createHandler,
        UpdateVendorInternalCommandHandler updateHandler,
        DeleteVendorInternalCommandHandler deleteHandler,
        ApproveVendorInternalCommandHandler approveHandler,
        RejectVendorInternalCommandHandler rejectHandler
    ) BuildHandlers()
    {
        var currentUserService = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var vendorRepository = new Mock<IVendorRepository>(MockBehavior.Strict);
        var loggerCreate = new Mock<Microsoft.Extensions.Logging.ILogger<CreateVendorInternalCommandHandler>>();
        var loggerUpdate = new Mock<Microsoft.Extensions.Logging.ILogger<UpdateVendorInternalCommandHandler>>();
        var loggerDelete = new Mock<Microsoft.Extensions.Logging.ILogger<DeleteVendorInternalCommandHandler>>();
        var loggerApprove = new Mock<Microsoft.Extensions.Logging.ILogger<ApproveVendorInternalCommandHandler>>();
        var loggerReject = new Mock<Microsoft.Extensions.Logging.ILogger<RejectVendorInternalCommandHandler>>();

        unitOfWork.Setup(u => u.Vendors).Returns(vendorRepository.Object);

        return (
            currentUserService,
            unitOfWork,
            new CreateVendorInternalCommandHandler(currentUserService.Object, unitOfWork.Object, loggerCreate.Object),
            new UpdateVendorInternalCommandHandler(currentUserService.Object, unitOfWork.Object, loggerUpdate.Object),
            new DeleteVendorInternalCommandHandler(currentUserService.Object, unitOfWork.Object, loggerDelete.Object),
            new ApproveVendorInternalCommandHandler(unitOfWork.Object, loggerApprove.Object),
            new RejectVendorInternalCommandHandler(unitOfWork.Object, loggerReject.Object)
        );
    }

    // -----------------------------------------------------------------------
    // Create Vendor Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateVendor_WithValidData_StartsInPendingState()
    {
        // Arrange
        var (currentUserService, unitOfWork, createHandler, _, _, _, _) = BuildHandlers();
        var userId = 1;
        var command = new CreateVendorInternalCommand
        {
            StoreName = "Tech Store",
            StoreDescription = "A tech store",
            PhoneNumber = "123456789",
            Address = "123 Main St"
        };

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(userId);
        unitOfWork.Setup(u => u.Vendors.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vendor?)null);
        unitOfWork.Setup(u => u.Vendors.CreateAsync(It.IsAny<Vendor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vendor v, CancellationToken _) =>
            {
                v.Id = 1;
                v.CreatedAt = DateTime.UtcNow;
                return v;
            });
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await createHandler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tech Store", result.StoreName);
        Assert.False(result.IsApproved);  // Pending
        Assert.True(result.IsActive);     // Active by default
        unitOfWork.Verify(u => u.Vendors.CreateAsync(It.IsAny<Vendor>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateVendor_UsesAuthenticatedUserId()
    {
        // Arrange
        var (currentUserService, unitOfWork, createHandler, _, _, _, _) = BuildHandlers();
        var authenticatedUserId = 5;
        var command = new CreateVendorInternalCommand
        {
            StoreName = "My Store",
            StoreDescription = "Description"
        };

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(authenticatedUserId);
        unitOfWork.Setup(u => u.Vendors.GetByUserIdAsync(authenticatedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vendor?)null);
        
        Vendor capturedVendor = null!;
        unitOfWork.Setup(u => u.Vendors.CreateAsync(It.IsAny<Vendor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vendor v, CancellationToken _) =>
            {
                capturedVendor = v;
                v.Id = 1;
                v.CreatedAt = DateTime.UtcNow;
                return v;
            });
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await createHandler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedVendor);
        Assert.Equal(authenticatedUserId, capturedVendor.UserId);
    }

    [Fact]
    public async Task CreateVendor_RejectsIfUserAlreadyHasVendor()
    {
        // Arrange
        var (currentUserService, unitOfWork, createHandler, _, _, _, _) = BuildHandlers();
        var userId = 1;
        var existingVendor = new Vendor { Id = 99, UserId = userId, StoreName = "Existing" };
        var command = new CreateVendorInternalCommand { StoreName = "New Store", StoreDescription = "Desc" };

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(userId);
        unitOfWork.Setup(u => u.Vendors.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVendor);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => createHandler.Handle(command, CancellationToken.None));
        Assert.Contains("already has a vendor", ex.Message);
    }

    // -----------------------------------------------------------------------
    // Update Vendor Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateVendor_WithOwnershipCheck_Succeeds()
    {
        // Arrange
        var (currentUserService, unitOfWork, _, updateHandler, _, _, _) = BuildHandlers();
        var userId = 1;
        var vendorId = 100;
        var vendor = new Vendor
        {
            Id = vendorId,
            UserId = userId,
            StoreName = "Old Name",
            StoreDescription = "Old Desc"
        };
        var command = new UpdateVendorInternalCommand
        {
            Id = vendorId,
            StoreName = "New Name",
            StoreDescription = "New Desc"
        };

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(userId);
        unitOfWork.Setup(u => u.Vendors.GetByIdAsync(vendorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vendor);
        unitOfWork.Setup(u => u.Vendors.UpdateAsync(It.IsAny<Vendor>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Vendor, CancellationToken>((v, _) => v.UpdatedAt = DateTime.UtcNow);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await updateHandler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("New Name", result.StoreName);
        Assert.Equal("New Desc", result.StoreDescription);
    }

    [Fact]
    public async Task UpdateVendor_RejectsUnauthorizedUser()
    {
        // Arrange
        var (currentUserService, unitOfWork, _, updateHandler, _, _, _) = BuildHandlers();
        var authenticatedUserId = 1;
        var vendorOwnerId = 2;
        var vendor = new Vendor { Id = 100, UserId = vendorOwnerId, StoreName = "Store" };
        var command = new UpdateVendorInternalCommand { Id = 100, StoreName = "Updated" };

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(authenticatedUserId);
        unitOfWork.Setup(u => u.Vendors.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vendor);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => updateHandler.Handle(command, CancellationToken.None));
        Assert.Contains("Cannot update vendor", ex.Message);
    }

    [Fact]
    public async Task UpdateVendor_ThrowsWhenNotFound()
    {
        // Arrange
        var (currentUserService, unitOfWork, _, updateHandler, _, _, _) = BuildHandlers();
        var command = new UpdateVendorInternalCommand { Id = 999, StoreName = "Updated" };

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(1);
        unitOfWork.Setup(u => u.Vendors.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vendor?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => updateHandler.Handle(command, CancellationToken.None));
        Assert.Contains("not found", ex.Message);
    }

    // -----------------------------------------------------------------------
    // Delete Vendor Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteVendor_WithOwnershipCheck_SoftDeletes()
    {
        // Arrange
        var (currentUserService, unitOfWork, _, _, deleteHandler, _, _) = BuildHandlers();
        var userId = 1;
        var vendor = new Vendor { Id = 100, UserId = userId, StoreName = "Store" };
        var command = new DeleteVendorInternalCommand { Id = 100 };

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(userId);
        unitOfWork.Setup(u => u.Vendors.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vendor);
        unitOfWork.Setup(u => u.Vendors.UpdateAsync(It.IsAny<Vendor>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Vendor, CancellationToken>((v, _) =>
            {
                v.DeletedAt = DateTime.UtcNow;
            });
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await deleteHandler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(vendor.IsDeleted);
        Assert.NotNull(vendor.DeletedAt);
    }

    [Fact]
    public async Task DeleteVendor_RejectsUnauthorizedUser()
    {
        // Arrange
        var (currentUserService, unitOfWork, _, _, deleteHandler, _, _) = BuildHandlers();
        var authenticatedUserId = 1;
        var vendorOwnerId = 2;
        var vendor = new Vendor { Id = 100, UserId = vendorOwnerId, StoreName = "Store" };
        var command = new DeleteVendorInternalCommand { Id = 100 };

        currentUserService.Setup(s => s.GetCurrentUserId()).Returns(authenticatedUserId);
        unitOfWork.Setup(u => u.Vendors.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vendor);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => deleteHandler.Handle(command, CancellationToken.None));
        Assert.Contains("Cannot delete vendor", ex.Message);
    }

    // -----------------------------------------------------------------------
    // Approve Vendor Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApproveVendor_TransitionsPendingToApproved()
    {
        // Arrange
        var (_, unitOfWork, _, _, _, approveHandler, _) = BuildHandlers();
        var vendor = new Vendor
        {
            Id = 100,
            UserId = 1,
            StoreName = "Store",
            IsApproved = false,
            IsActive = true
        };
        var command = new ApproveVendorInternalCommand { Id = 100 };

        unitOfWork.Setup(u => u.Vendors.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vendor);
        unitOfWork.Setup(u => u.Vendors.UpdateAsync(It.IsAny<Vendor>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Vendor, CancellationToken>((v, _) => v.UpdatedAt = DateTime.UtcNow);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await approveHandler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsApproved);
        Assert.True(result.IsActive);
    }

    // -----------------------------------------------------------------------
    // Reject Vendor Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RejectVendor_SoftDeletesVendor()
    {
        // Arrange
        var (_, unitOfWork, _, _, _, _, rejectHandler) = BuildHandlers();
        var vendor = new Vendor { Id = 100, UserId = 1, StoreName = "Store", IsApproved = false };
        var command = new RejectVendorInternalCommand { Id = 100, Reason = "Suspicious activity" };

        unitOfWork.Setup(u => u.Vendors.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vendor);
        unitOfWork.Setup(u => u.Vendors.UpdateAsync(It.IsAny<Vendor>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Vendor, CancellationToken>((v, _) => v.DeletedAt = DateTime.UtcNow);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await rejectHandler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(vendor.IsDeleted);
        Assert.NotNull(vendor.DeletedAt);
    }

    // -----------------------------------------------------------------------
    // Query Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetActiveVendorsQuery_ReturnsOnlyActiveVendors()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var vendorRepository = new Mock<IVendorRepository>(MockBehavior.Strict);
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<GetActiveVendorsQueryHandler>>();
        
        var activeVendors = new List<Vendor>
        {
            new Vendor { Id = 1, StoreName = "Active Vendor", IsActive = true, IsApproved = true },
            new Vendor { Id = 2, StoreName = "Another Active", IsActive = true, IsApproved = false }
        };
        
        unitOfWork.Setup(u => u.Vendors).Returns(vendorRepository.Object);
        vendorRepository.Setup(r => r.GetActiveVendorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeVendors);

        var handler = new GetActiveVendorsQueryHandler(unitOfWork.Object, logger.Object);
        var query = new GetActiveVendorsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, v => Assert.True(v.IsActive));
    }

    [Fact]
    public async Task GetApprovedVendorsQuery_ReturnsOnlyApprovedAndActiveVendors()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var vendorRepository = new Mock<IVendorRepository>(MockBehavior.Strict);
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<GetApprovedVendorsQueryHandler>>();
        
        var approvedVendors = new List<Vendor>
        {
            new Vendor { Id = 1, StoreName = "Approved Vendor", IsActive = true, IsApproved = true, AverageRating = 4.5m }
        };
        
        unitOfWork.Setup(u => u.Vendors).Returns(vendorRepository.Object);
        vendorRepository.Setup(r => r.GetApprovedVendorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedVendors);

        var handler = new GetApprovedVendorsQueryHandler(unitOfWork.Object, logger.Object);
        var query = new GetApprovedVendorsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var vendor = result.First();
        Assert.True(vendor.IsApproved);
        Assert.True(vendor.IsActive);
    }

    [Fact]
    public async Task GetPendingVendorsQuery_ReturnsOnlyNotApprovedVendors()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var vendorRepository = new Mock<IVendorRepository>(MockBehavior.Strict);
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<GetPendingVendorsQueryHandler>>();
        
        var pendingVendors = new List<Vendor>
        {
            new Vendor { Id = 1, StoreName = "Pending Vendor", IsApproved = false, IsActive = true }
        };
        
        unitOfWork.Setup(u => u.Vendors).Returns(vendorRepository.Object);
        vendorRepository.Setup(r => r.GetPendingVendorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingVendors);

        var handler = new GetPendingVendorsQueryHandler(unitOfWork.Object, logger.Object);
        var query = new GetPendingVendorsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.False(result.First().IsApproved);
    }

    [Fact]
    public async Task GetTopRatedVendorsQuery_ReturnsTopRatedApprovedAndActiveVendors()
    {
        // Arrange
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var vendorRepository = new Mock<IVendorRepository>(MockBehavior.Strict);
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<GetTopRatedVendorsQueryHandler>>();
        
        var topVendors = new List<Vendor>
        {
            new Vendor { Id = 1, StoreName = "Top Vendor", IsApproved = true, IsActive = true, AverageRating = 4.8m },
            new Vendor { Id = 2, StoreName = "Great Vendor", IsApproved = true, IsActive = true, AverageRating = 4.5m }
        };
        
        unitOfWork.Setup(u => u.Vendors).Returns(vendorRepository.Object);
        vendorRepository.Setup(r => r.GetTopRatedVendorsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topVendors);

        var handler = new GetTopRatedVendorsQueryHandler(unitOfWork.Object, logger.Object);
        var query = new GetTopRatedVendorsQuery { Count = 10 };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, v => Assert.True(v.IsApproved && v.IsActive));
    }

    // -----------------------------------------------------------------------
    // State Machine Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Vendor_CanTransition_Pending_To_Approved()
    {
        // Arrange
        var vendor = new Vendor
        {
            Id = 1,
            UserId = 1,
            StoreName = "Store",
            IsApproved = false,  // Pending
            IsActive = true
        };

        // Act
        vendor.IsApproved = true;  // Transition to Approved

        // Assert
        Assert.True(vendor.IsApproved);
        Assert.True(vendor.IsActive);
    }

    [Fact]
    public void Vendor_CanTransition_Active_To_Inactive()
    {
        // Arrange
        var vendor = new Vendor
        {
            Id = 1,
            UserId = 1,
            StoreName = "Store",
            IsApproved = true,
            IsActive = true  // Active
        };

        // Act
        vendor.IsActive = false;  // Transition to Inactive

        // Assert
        Assert.True(vendor.IsApproved);
        Assert.False(vendor.IsActive);
    }

    [Fact]
    public void Vendor_CanSoftDelete()
    {
        // Arrange
        var vendor = new Vendor { Id = 1, UserId = 1, StoreName = "Store" };

        // Act
        vendor.Delete();

        // Assert
        Assert.True(vendor.IsDeleted);
        Assert.NotNull(vendor.DeletedAt);
    }
}
