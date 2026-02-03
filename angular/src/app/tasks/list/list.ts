import { Component, OnInit, inject } from '@angular/core';
import { ListService, PagedResultDto } from '@abp/ng.core';
import { TaskService, TaskDto, TaskStatus } from 'src/app/proxy/tasks';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { UserLookupDto } from 'src/app/proxy/tasks';

@Component({
  selector: 'app-list',
  templateUrl: './list.html',
  styleUrls: ['./list.scss'],
  providers: [ListService],
  // eslint-disable-next-line @angular-eslint/prefer-standalone
  standalone: false,
})
export class List implements OnInit {
  // Dữ liệu hiển thị
  taskData: PagedResultDto<TaskDto> = { items: [], totalCount: 0 };
  users: UserLookupDto[] = []; // Danh sách user
  taskStatus = TaskStatus;

  // Biến trạng thái
  loading = false;
  saving = false;
  isModalOpen = false;
  isEditMode = false;
  selectedTaskId: string = '';

  // Biến bộ lọc (Filter)
  filterStatus: TaskStatus | null = null;
  filterAssignedUserId: string | null = null;
  pageSize = 10;
  pageIndex = 1;

  form: FormGroup;

  // Inject Services
  public readonly list = inject(ListService);
  private readonly taskService = inject(TaskService);
  private readonly fb = inject(FormBuilder);
  private readonly message = inject(NzMessageService);
  private readonly confirmation = inject(ConfirmationService);

  ngOnInit() {
    this.taskService.getUserLookup().subscribe(result => {
      this.users = result.items;
    });
    this.buildForm();
    // Hook request lấy danh sách
    const taskStreamCreator = query => {
      this.loading = true;
      return this.taskService.getList({
        ...query,
        status: this.filterStatus, // Gửi kèm filter status
        assignedUserId: this.filterAssignedUserId, // Gửi kèm filter user
      });
    };

    this.list.hookToQuery(taskStreamCreator).subscribe(response => {
      this.taskData = response;
      this.loading = false;
    });
  }

  // --- HÀM XỬ LÝ LỌC & PHÂN TRANG ---
  onFilterChange() {
    this.list.get(); // Gọi lại API khi thay đổi bộ lọc
  }

  clearFilters() {
    this.filterStatus = null;
    this.filterAssignedUserId = null;
    this.list.get();
  }

  onPageChange(pageIndex: number) {
    this.pageIndex = pageIndex;
    this.list.page = pageIndex - 1; // Antd đếm từ 1, ABP đếm từ 0
  }

  onPageSizeChange(pageSize: number) {
    this.pageSize = pageSize;
    this.list.maxResultCount = pageSize;
  }

  // --- HÀM XỬ LÝ MODAL (TẠO/SỬA) ---
  buildForm() {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(256)]],
      description: [null],
      status: [TaskStatus.New, [Validators.required]],
      assignedUserId: [null],
    });
  }

  createTask() {
    this.isEditMode = false;
    this.selectedTaskId = '';
    this.form.reset({ status: TaskStatus.New }); // Reset form về mặc định
    this.isModalOpen = true;
  }

  editTask(task: TaskDto) {
    this.isEditMode = true;
    this.selectedTaskId = task.id;
    this.form.patchValue(task); // Đổ dữ liệu cũ vào form
    this.isModalOpen = true;
  }

  handleCancel() {
    this.isModalOpen = false;
  }

  save() {
    if (this.form.invalid) return;

    this.saving = true;
    const request = this.isEditMode
      ? this.taskService.update(this.selectedTaskId, this.form.value)
      : this.taskService.create(this.form.value);

    request.subscribe({
      next: () => {
        this.message.success(this.isEditMode ? 'Cập nhật thành công!' : 'Tạo mới thành công!');
        this.isModalOpen = false;
        this.list.get();
        this.saving = false;
      },
      error: () => {
        this.saving = false;
      },
    });
  }

  // --- HÀM XÓA ---
  delete(id: string) {
    this.confirmation.warn('::AreYouSureToDelete', '::ConfirmDelete').subscribe(status => {
      if (status === Confirmation.Status.confirm) {
        this.taskService.delete(id).subscribe(() => {
          this.message.success('Đã xóa thành công!');
          this.list.get();
        });
      }
    });
  }

  // --- HELPER UI ---
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
        return 'New';
      case TaskStatus.InProgress:
        return 'In Progress';
      case TaskStatus.Completed:
        return 'Completed';
      default:
        return '';
    }
  }
}
