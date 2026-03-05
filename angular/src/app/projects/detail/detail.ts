import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ListService, PagedResultDto, ConfigStateService } from '@abp/ng.core';
import { ProjectService, ProjectDto } from '@proxy/projects';
import { TaskService, TaskDto, TaskStatus, UserLookupDto } from '@proxy/tasks';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Confirmation, ConfirmationService } from '@abp/ng.theme.shared';
import { LocalizationService } from '@abp/ng.core';

@Component({
  selector: 'app-detail',
  templateUrl: './detail.html',
  styleUrls: ['./detail.scss'],
  providers: [ListService],
  // eslint-disable-next-line @angular-eslint/prefer-standalone
  standalone: false,
})
export class DetailComponent implements OnInit {
  projectId: string;
  project: ProjectDto;

  // Dữ liệu bảng Task
  taskData: PagedResultDto<TaskDto> = { items: [], totalCount: 0 };
  users: UserLookupDto[] = [];
  taskStatus = TaskStatus;

  // Trạng thái UI
  loading = false;
  isModalOpen = false;
  isEditMode = false;
  selectedTaskId = '';
  form: FormGroup;
  currentUserId: string;
  isAdmin = false;

  // Services
  private route = inject(ActivatedRoute);
  private projectService = inject(ProjectService);
  private taskService = inject(TaskService);
  public list = inject(ListService);
  private fb = inject(FormBuilder);
  private message = inject(NzMessageService);
  private confirmation = inject(ConfirmationService);
  private config = inject(ConfigStateService);
  private localizationService = inject(LocalizationService);

  isOverdueModalVisible = false;
  isPendingModalVisible = false;
  filterTaskText = '';
  filterTaskStatus: number | null = null;
  filterTaskUserIds: string[] = [];

  pageIndex = 1;
  pageSize = 10;
  totalCount = 0;

  projectStats = { total: 0, inProgress: 0, completed: 0, overdue: 0, pending: 0 };

  // Xử lý bộ lọc công việc quá hạn
  overdueFilterStatus: 'all' | 'incomplete' | 'completed' = 'all';
  allOverdueTasks: TaskDto[] = [];
  isLoadingOverdue = false;

  showOverdueModal() {
    this.isOverdueModalVisible = true;
    this.isLoadingOverdue = true;
    this.overdueFilterStatus = 'all'; // Reset bộ lọc về Tất cả

    // Gọi API lấy tối đa 1000 task để không bị sót dữ liệu của dự án
    this.taskService
      .getList({
        projectId: this.projectId,
        maxResultCount: 1000,
        skipCount: 0,
      })
      .subscribe(res => {
        const now = new Date().getTime();
        // Vét toàn bộ task có hạn chót < hiện tại
        this.allOverdueTasks = res.items.filter(
          t => t.dueDate && new Date(t.dueDate).getTime() < now,
        );
        this.isLoadingOverdue = false;
      });
  }

  get displayOverdueTasks() {
    if (this.overdueFilterStatus === 'incomplete') {
      return this.allOverdueTasks.filter(t => t.status !== this.taskStatus.Completed);
    }
    if (this.overdueFilterStatus === 'completed') {
      return this.allOverdueTasks.filter(t => t.status === this.taskStatus.Completed);
    }
    return this.allOverdueTasks;
  }

  // Phân trang
  pageIndexChange(index: number) {
    this.list.page = index - 1; // ABP tính trang từ 0, Ng-Zorro tính từ 1
  }

  loadData() {
    this.loading = true;
    const requestPayload = {
      projectId: this.projectId,
      maxResultCount: this.pageSize,
      skipCount: (this.pageIndex - 1) * this.pageSize,

      filterText: this.filterTaskText,
      status: this.filterTaskStatus,
      assignedUserId: this.filterTaskUserIds.length > 0 ? this.filterTaskUserIds[0] : null,
    };

    this.taskService.getList(requestPayload).subscribe({
      next: (res: any) => {
        this.taskData = res;
        this.totalCount = res.totalCount || res.TotalCount || 0;
        this.loading = false;
      },
    });
  }

  get projectAssignees() {
    const assigneesMap = new Map<string, string>();
    const items = this.taskData?.items || [];

    // Quét qua toàn bộ task, gom nhặt ID và Tên của những người được giao việc
    items.forEach(task => {
      if (task.assignedUserIds && task.assignedUserIds.length > 0) {
        task.assignedUserIds.forEach((id: string, index: number) => {
          // Lấy tên tương ứng, nếu không có thì để tạm là 'Unknown User'
          const name = task.assignedUserNames ? task.assignedUserNames[index] : 'Unknown User';
          assigneesMap.set(id, name);
        });
      }
    });

    // Trả về một mảng gọn gàng để binding lên dropdown
    return Array.from(assigneesMap.entries()).map(([id, name]) => ({ id, name }));
  }

  // 3. Getter tự động lọc danh sách công việc
  get filteredTasks() {
    let tasks = this.taskData?.items || [];

    // Lọc theo từ khóa (Tìm trong Tiêu đề)
    if (this.filterTaskText) {
      const text = this.filterTaskText.toLowerCase();
      tasks = tasks.filter(t => t.title?.toLowerCase().includes(text));
    }

    // Lọc theo Trạng thái (Phải check khác null/undefined vì status 0 có thể bị tính là false)
    if (this.filterTaskStatus !== null && this.filterTaskStatus !== undefined) {
      tasks = tasks.filter(t => t.status === this.filterTaskStatus);
    }

    // Lọc theo Nhiều Người thực hiện
    if (this.filterTaskUserIds && this.filterTaskUserIds.length > 0) {
      tasks = tasks.filter(t => {
        // Nếu task này không có ai làm -> chắc chắn không thỏa mãn -> loại
        if (!t.assignedUserIds || t.assignedUserIds.length === 0) return false;

        // Trả về true CHỈ KHI toàn bộ các ID trong bộ lọc đều nằm trong danh sách người làm task
        return this.filterTaskUserIds.every(filterId => t.assignedUserIds.includes(filterId));
      });
    }

    return tasks;
  }

  get pendingTasks() {
    const items = this.taskData.items || [];
    return items.filter(t => !t.isApproved);
  }

  get stats() {
    return this.projectStats;
  }

  getStatusColor(status: TaskStatus): string {
    switch (status) {
      case TaskStatus.New:
        return 'blue';
      case TaskStatus.InProgress:
        return 'orange';
      case TaskStatus.Completed:
        return 'green';
      default:
        return 'default';
    }
  }

  getStatusText(status: TaskStatus): string {
    switch (status) {
      case TaskStatus.New:
        return '::Enum:TaskStatus.0';
      case TaskStatus.InProgress:
        return '::Enum:TaskStatus.1';
      case TaskStatus.Completed:
        return '::Enum:TaskStatus.2';
      default:
        return '';
    }
  }

  get projectMembers(): UserLookupDto[] {
    if (!this.project || !this.users) return [];

    // Chỉ lấy những User là Manager HOẶC nằm trong danh sách MemberIds của dự án
    return this.users.filter(
      u =>
        u.id === this.project.managerId ||
        (this.project.memberIds && this.project.memberIds.includes(u.id)),
    );
  }

  get progress() {
    const allTasks = this.taskData.items || [];
    if (allTasks.length === 0) return 0;

    const completedCount = allTasks.filter(t => t.status === this.taskStatus.Completed).length;
    return Math.round((completedCount / allTasks.length) * 100);
  }

  ngOnInit() {
    this.projectId = this.route.snapshot.params['id']; // Lấy ID từ URL
    const currentUser = this.config.getOne('currentUser');
    this.currentUserId = currentUser.id;
    this.isAdmin = currentUser.roles.includes('admin');

    this.loadProject();
    this.loadUsers();
    this.buildForm();

    const streamCreator = query => {
      this.loading = true;
      this.taskService.getProjectStats(this.projectId).subscribe(res => {
        this.projectStats = {
          total: res.totalTasks,
          inProgress: res.inProgressTasks,
          completed: res.completedTasks,
          overdue: res.overdueTasks,
          pending: res.pendingTasks,
        };
      });

      return this.taskService.getList({
        ...query,
        projectId: this.projectId,
        filterText: this.filterTaskText,
        status: this.filterTaskStatus,
        assignedUserId: this.filterTaskUserIds.length > 0 ? this.filterTaskUserIds[0] : undefined,
      });
    };

    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.taskData = res;
      this.loading = false;
    });
  }

  loadProject() {
    this.projectService.get(this.projectId).subscribe(res => (this.project = res));
  }

  loadUsers() {
    this.taskService.getUserLookup().subscribe(res => (this.users = res.items));
  }

  buildForm() {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(256)]],
      description: [null],
      status: [TaskStatus.New, Validators.required],
      dueDate: [null, [Validators.required]],
      assignedUserIds: [[]],
      projectId: [this.projectId],
    });
  }

  // --- ACTIONS ---
  approveTask(id: string) {
    this.taskService.approveTask(id).subscribe(() => {
      const translatedMessage = this.localizationService.instant('::AcceptTaskSuccess');
      this.message.success(translatedMessage);
      this.list.get();
    });
  }

  createTask() {
    this.isEditMode = false;
    this.form.enable();
    this.form.reset({ status: TaskStatus.New, projectId: this.projectId });
    this.isModalOpen = true;
  }

  editTask(task: TaskDto) {
    this.isEditMode = true;
    this.selectedTaskId = task.id;

    let dueDate = null;
    if (task.dueDate) {
      const dateStr = task.dueDate.endsWith('Z') ? task.dueDate : task.dueDate + 'Z';
      const utcDate = new Date(dateStr);
      if (!isNaN(utcDate.getTime())) {
        const localDate = new Date(utcDate.getTime() - utcDate.getTimezoneOffset() * 60000);
        dueDate = localDate.toISOString().slice(0, 16);
      }
    }

    this.form.patchValue({
      ...task,
      dueDate,
      assignedUserIds: task.assignedUserIds || [],
    });

    // Nếu đã duyệt, disable các ô nhập liệu không được sửa
    if (task.isApproved) {
      this.form.get('title').disable();
      this.form.get('description').disable();
      this.form.get('dueDate').disable();
    } else {
      // Nếu chưa duyệt, mở khóa lại toàn bộ
      this.form.enable();
    }

    this.isModalOpen = true;
  }

  save() {
    if (this.form.invalid) return;

    //Dùng getRawValue() thay vì form.value để lấy được cả dữ liệu của các ô bị disable
    const input = this.form.getRawValue();

    if (input.dueDate) {
      // Gửi lên dưới dạng UTC ISO string
      input.dueDate = new Date(input.dueDate).toISOString();
    }

    const request = this.isEditMode
      ? this.taskService.update(this.selectedTaskId, input)
      : this.taskService.create(input);

    request.subscribe(() => {
      this.isModalOpen = false;
      this.list.get();
    });
  }

  rejectTask(id: string) {
    // Tái sử dụng API xóa task
    this.taskService.delete(id).subscribe(() => {
      const translatedMessage = this.localizationService.instant('::RejectTaskSuccess');

      this.message.success(translatedMessage);
      this.list.get();
    });
  }
  deleteTask(id: string) {
    const translatedMessage = this.localizationService.instant('::ConfirmDelete');
    const translatedTitle = this.localizationService.instant('::AreYouSureToDelete');
    this.confirmation.warn(translatedTitle, translatedMessage).subscribe(status => {
      if (status === Confirmation.Status.confirm) {
        this.taskService.delete(id).subscribe(() => this.list.get());
      }
    });
  }

  // Phân quyền hiển thị nút Duyệt
  canApprove(task: TaskDto): boolean {
    return !task.isApproved && (this.isAdmin || this.project?.managerId === this.currentUserId);
  }

  canUpdate(task: TaskDto): boolean {
    return (
      this.isAdmin ||
      this.project?.managerId === this.currentUserId ||
      task.creatorId === this.currentUserId ||
      task.assignedUserIds?.includes(this.currentUserId)
    );
  }

  canDelete(task: TaskDto): boolean {
    return (
      this.isAdmin ||
      this.project?.managerId === this.currentUserId ||
      task.creatorId === this.currentUserId
    );
  }

  clearFilters() {
    this.filterTaskText = '';
    this.filterTaskStatus = null;
    this.filterTaskUserIds = [];
  }
}
