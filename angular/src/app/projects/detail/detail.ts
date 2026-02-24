import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ListService, PagedResultDto, ConfigStateService } from '@abp/ng.core';
import { ProjectService, ProjectDto } from '@proxy/projects';
import { TaskService, TaskDto, TaskStatus, UserLookupDto } from '@proxy/tasks';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Confirmation, ConfirmationService } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-detail',
  templateUrl: './detail.html',
  styleUrls: ['./detail.scss'],
  providers: [ListService], // Giúp quản lý phân trang/sắp xếp bảng task
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

  // --- THỐNG KÊ (TÍNH TOÁN TẠI FRONTEND) ---
  get stats() {
    const items = this.taskData.items || [];
    return {
      total: items.length,
      inProgress: items.filter(t => t.status === TaskStatus.InProgress).length,
      completed: items.filter(t => t.status === TaskStatus.Completed).length,
      overdue: items.filter(
        t => t.dueDate && new Date(t.dueDate) < new Date() && t.status !== TaskStatus.Completed,
      ).length,
      pending: items.filter(t => !t.isApproved).length,
    };
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

    // Hook vào bảng Task, tự động lọc theo ProjectId này
    const streamCreator = query => {
      this.loading = true;
      return this.taskService.getList({ ...query, projectId: this.projectId });
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
      dueDate: [null],
      assignedUserIds: [[]],
      projectId: [this.projectId],
    });
  }

  // --- ACTIONS ---
  approveTask(id: string) {
    this.taskService.approveTask(id).subscribe(() => {
      this.message.success('Đã phê duyệt công việc!');
      this.list.get(); // Refresh bảng
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
      this.message.success('Đã từ chối và xóa công việc!');
      this.list.get();
    });
  }

  deleteTask(id: string) {
    this.confirmation.warn('::AreYouSureToDelete', '::ConfirmDelete').subscribe(status => {
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
}
