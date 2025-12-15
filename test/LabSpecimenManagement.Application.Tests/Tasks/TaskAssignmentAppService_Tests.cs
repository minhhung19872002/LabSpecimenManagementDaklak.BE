using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Tasks;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Modularity;
using Xunit;

namespace LabSpecimenManagement.Tasks;

public abstract class TaskAssignmentAppService_Tests<TStartupModule> : LabSpecimenManagementApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly ITaskAssignmentAppService _taskAssignmentAppService;

    protected TaskAssignmentAppService_Tests()
    {
        _taskAssignmentAppService = GetRequiredService<ITaskAssignmentAppService>();
    }

    [Fact]
    public async Task Should_Get_List_Of_TaskAssignments()
    {
        // Act
        var result = await _taskAssignmentAppService.GetListAsync(new PagedAndSortedResultRequestDto());

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Should_Get_TaskAssignment_By_Id()
    {
        // Act
        var result = await _taskAssignmentAppService.GetAsync(LabSpecimenManagementTestData.TaskAssignment1Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(LabSpecimenManagementTestData.TaskAssignment1Id);
        result.TaskNumber.ShouldBe("TASK-20240101-0001");
        result.Title.ShouldBe("Thực hiện phép thử pH");
        result.Type.ShouldBe(TaskType.Adhoc);
        result.Priority.ShouldBe(TaskPriority.High);
    }

    [Fact]
    public async Task Should_Create_TaskAssignment()
    {
        // Arrange
        var assigneeId = Guid.NewGuid();
        var input = new CreateTaskAssignmentDto
        {
            Title = "Phân tích mẫu nước mới",
            Description = "Thực hiện phân tích các chỉ tiêu nước theo yêu cầu",
            Type = TaskType.Adhoc,
            Priority = TaskPriority.Medium,
            AssignedToId = assigneeId,
            Department = "Phòng Hóa học",
            DueDate = DateTime.Now.AddDays(3),
            Notes = "Cần hoàn thành trước cuối tuần"
        };

        // Act
        var result = await _taskAssignmentAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldNotBe(Guid.Empty);
        result.TaskNumber.ShouldStartWith("TASK-");
        result.Title.ShouldBe("Phân tích mẫu nước mới");
        result.Type.ShouldBe(TaskType.Adhoc);
        result.Priority.ShouldBe(TaskPriority.Medium);
        result.Status.ShouldBe(TaskAssignmentStatus.Pending);
        result.Progress.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Update_TaskAssignment()
    {
        // Arrange
        var input = new UpdateTaskAssignmentDto
        {
            Title = "Thực hiện phép thử pH - Updated",
            Description = "Mô tả đã cập nhật",
            Priority = TaskPriority.High,
            Notes = "Ghi chú đã cập nhật"
        };

        // Act
        var result = await _taskAssignmentAppService.UpdateAsync(LabSpecimenManagementTestData.TaskAssignment1Id, input);

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("Thực hiện phép thử pH - Updated");
    }

    [Fact]
    public async Task Should_Accept_TaskAssignment()
    {
        // Create a new task to accept
        var newTask = await _taskAssignmentAppService.CreateAsync(new CreateTaskAssignmentDto
        {
            Title = "Task to accept",
            Type = TaskType.Adhoc,
            Priority = TaskPriority.Low,
            AssignedToId = Guid.NewGuid(),
            Department = "Test",
            DueDate = DateTime.Now.AddDays(1)
        });

        // Act
        var result = await _taskAssignmentAppService.AcceptAsync(newTask.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(TaskAssignmentStatus.Accepted);
    }

    [Fact]
    public async Task Should_Reject_TaskAssignment()
    {
        // Create a new task to reject
        var newTask = await _taskAssignmentAppService.CreateAsync(new CreateTaskAssignmentDto
        {
            Title = "Task to reject",
            Type = TaskType.Adhoc,
            Priority = TaskPriority.Low,
            AssignedToId = Guid.NewGuid(),
            Department = "Test",
            DueDate = DateTime.Now.AddDays(1)
        });

        // Act
        var result = await _taskAssignmentAppService.RejectAsync(newTask.Id, "Không đủ thời gian");

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(TaskAssignmentStatus.Rejected);
    }

    [Fact]
    public async Task Should_Start_TaskAssignment()
    {
        // Create and accept a new task to start
        var newTask = await _taskAssignmentAppService.CreateAsync(new CreateTaskAssignmentDto
        {
            Title = "Task to start",
            Type = TaskType.Adhoc,
            Priority = TaskPriority.Medium,
            AssignedToId = Guid.NewGuid(),
            Department = "Test",
            DueDate = DateTime.Now.AddDays(2)
        });

        await _taskAssignmentAppService.AcceptAsync(newTask.Id);

        // Act
        var result = await _taskAssignmentAppService.StartAsync(newTask.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(TaskAssignmentStatus.InProgress);
        result.StartDate.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Complete_TaskAssignment()
    {
        // Create, accept and start a new task to complete
        var newTask = await _taskAssignmentAppService.CreateAsync(new CreateTaskAssignmentDto
        {
            Title = "Task to complete",
            Type = TaskType.Adhoc,
            Priority = TaskPriority.High,
            AssignedToId = Guid.NewGuid(),
            Department = "Test",
            DueDate = DateTime.Now.AddDays(3)
        });

        await _taskAssignmentAppService.AcceptAsync(newTask.Id);
        await _taskAssignmentAppService.StartAsync(newTask.Id);

        // Act
        var result = await _taskAssignmentAppService.CompleteAsync(newTask.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(TaskAssignmentStatus.Completed);
        result.CompletedDate.ShouldNotBeNull();
        result.Progress.ShouldBe(100);
    }

    [Fact]
    public async Task Should_Update_TaskAssignment_Progress()
    {
        // Create and start a new task
        var newTask = await _taskAssignmentAppService.CreateAsync(new CreateTaskAssignmentDto
        {
            Title = "Task for progress update",
            Type = TaskType.Adhoc,
            Priority = TaskPriority.Medium,
            AssignedToId = Guid.NewGuid(),
            Department = "Test",
            DueDate = DateTime.Now.AddDays(2)
        });

        await _taskAssignmentAppService.AcceptAsync(newTask.Id);
        await _taskAssignmentAppService.StartAsync(newTask.Id);

        // Act
        var result = await _taskAssignmentAppService.UpdateProgressAsync(newTask.Id, 50);

        // Assert
        result.ShouldNotBeNull();
        result.Progress.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Get_Tasks_By_Assignee()
    {
        // Arrange
        var assigneeId = Guid.NewGuid();
        await _taskAssignmentAppService.CreateAsync(new CreateTaskAssignmentDto
        {
            Title = "Task for assignee 1",
            Type = TaskType.Adhoc,
            Priority = TaskPriority.Low,
            AssignedToId = assigneeId,
            Department = "Test",
            DueDate = DateTime.Now.AddDays(1)
        });

        await _taskAssignmentAppService.CreateAsync(new CreateTaskAssignmentDto
        {
            Title = "Task for assignee 2",
            Type = TaskType.Weekly,
            Priority = TaskPriority.Medium,
            AssignedToId = assigneeId,
            Department = "Test",
            DueDate = DateTime.Now.AddDays(2)
        });

        // Act
        var result = await _taskAssignmentAppService.GetByAssigneeAsync(assigneeId);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBeGreaterThanOrEqualTo(2);
        result.Items.ShouldAllBe(x => x.AssignedToId == assigneeId);
    }

    [Fact]
    public async Task Should_Delete_TaskAssignment()
    {
        // Create a new task to delete
        var newTask = await _taskAssignmentAppService.CreateAsync(new CreateTaskAssignmentDto
        {
            Title = "Task to delete",
            Type = TaskType.Adhoc,
            Priority = TaskPriority.Low,
            AssignedToId = Guid.NewGuid(),
            Department = "Test",
            DueDate = DateTime.Now.AddDays(1)
        });

        // Act & Assert
        await Should.NotThrowAsync(async () =>
        {
            await _taskAssignmentAppService.DeleteAsync(newTask.Id);
        });

        // Verify deleted
        await Should.ThrowAsync<Exception>(async () =>
        {
            await _taskAssignmentAppService.GetAsync(newTask.Id);
        });
    }

    [Fact]
    public async Task Should_Create_TaskAssignment_With_Related_Entities()
    {
        // Arrange
        var input = new CreateTaskAssignmentDto
        {
            Title = "Task with related entities",
            Type = TaskType.Adhoc,
            Priority = TaskPriority.High,
            AssignedToId = Guid.NewGuid(),
            Department = "Phòng Hóa học",
            DueDate = DateTime.Now.AddDays(5),
            RelatedTestRequestIds = $"[\"{LabSpecimenManagementTestData.TestRequest1Id}\"]",
            RelatedSpecimenIds = $"[\"{LabSpecimenManagementTestData.Specimen1Id}\"]",
            RelatedTestIds = $"[\"{LabSpecimenManagementTestData.Test1Id}\"]"
        };

        // Act
        var result = await _taskAssignmentAppService.CreateAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.RelatedTestRequestIds.ShouldNotBeNullOrEmpty();
        result.RelatedSpecimenIds.ShouldNotBeNullOrEmpty();
        result.RelatedTestIds.ShouldNotBeNullOrEmpty();
    }
}
