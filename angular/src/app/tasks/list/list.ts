import { Component, OnInit, inject } from '@angular/core';
import { ListService, PagedResultDto, ConfigStateService } from '@abp/ng.core';
import { TaskService, TaskDto, TaskStatus, UserLookupDto } from 'src/app/proxy/tasks';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NzTableQueryParams } from 'ng-zorro-antd/table';
import { ProjectService, ProjectDto } from '@proxy/projects';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-list',
  templateUrl: './list.html',
  styleUrls: ['./list.scss'],
  providers: [ListService],
  // eslint-disable-next-line @angular-eslint/prefer-standalone
  standalone: false,
})
export class List implements OnInit {
  // --- DỮ LIỆU ---
  taskData: PagedResultDto<TaskDto> = { items: [], totalCount: 0 };
  users: UserLookupDto[] = [];
  taskStatus = TaskStatus;
  projects: ProjectDto[] = [];

  // --- TRẠNG THÁI UI ---
  loading = false;
  saving = false;
  isModalOpen = false;
  isEditMode = false;
  selectedTaskId: string = '';

  // --- BỘ LỌC & SẮP XẾP ---
  filterText = ''; // Biến tìm kiếm
  filterStatus: TaskStatus | null = null;
  filterAssignedUserId: string | null = null;
  filterProjectId: string | null = null;
  sorting = ''; // Biến lưu chuỗi sắp xếp (vd: "Title ASC")

  pageSize = 10;
  pageIndex = 1;

  form: FormGroup;
  currentUserId: string;
  isAdmin: boolean;

  // --- INJECT SERVICES ---
  private readonly route = inject(ActivatedRoute);
  public readonly list = inject(ListService);
  private readonly taskService = inject(TaskService);
  private readonly fb = inject(FormBuilder);
  private readonly message = inject(NzMessageService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly config = inject(ConfigStateService);
  private readonly projectService = inject(ProjectService);

  ngOnInit() {
    // 1. Lấy thông tin User hiện tại
    const currentUser = this.config.getOne('currentUser');
    this.currentUserId = currentUser.id;
    this.isAdmin = currentUser.roles.includes('admin');

    // 2. Lấy danh sách User để hiển thị trong Dropdown
    this.taskService.getUserLookup().subscribe(result => {
      this.users = result.items;
    });

    this.loadProjects();

    // 3. Khởi tạo Form
    this.buildForm();

    // 4. CẤU HÌNH STREAM LẤY DỮ LIỆU (QUAN TRỌNG)
    const taskStreamCreator = query => {
      this.loading = true;
      return this.taskService.getList({
        ...query,
        // Truyền thêm các tham số bộ lọc của riêng mình
        filterText: this.filterText,
        status: this.filterStatus,
        assignedUserId: this.filterAssignedUserId,
        projectId: this.filterProjectId,
        sorting: this.sorting,
      });
    };

    this.list.hookToQuery(taskStreamCreator).subscribe(response => {
      this.taskData = response;
      this.loading = false;
    });
  }

  // --- HÀM XỬ LÝ TABLE (SORT & PAGING) ---

  // Hàm này tự động chạy khi: Đổi trang, Đổi số lượng dòng, hoặc Click vào tiêu đề cột để Sort
  onQueryParamsChange(params: NzTableQueryParams): void {
    const { pageSize, pageIndex, sort } = params;

    // Cập nhật biến local
    this.pageIndex = pageIndex;
    this.pageSize = pageSize;

    // Cập nhật cấu hình cho ListService của ABP
    this.list.maxResultCount = pageSize;
    this.list.page = pageIndex - 1; // ABP tính trang từ 0, Antd tính từ 1

    // Xử lý Logic Sắp xếp
    const currentSort = sort.find(item => item.value !== null);
    if (currentSort) {
      // Chuyển "ascend" -> "ASC", "descend" -> "DESC"
      const sortDirection = currentSort.value === 'ascend' ? 'ASC' : 'DESC';
      this.sorting = `${currentSort.key} ${sortDirection}`;
    } else {
      this.sorting = ''; // Không sắp xếp
    }

    // Gọi API (Refresh lại bảng)
    this.list.get();
  }

  // --- HÀM TÌM KIẾM & BỘ LỌC ---

  // Khi ấn Enter ở ô tìm kiếm
  onSearch() {
    this.list.page = 0; // Reset về trang 1
    this.list.get();
  }

  // Khi chọn Dropdown Status hoặc User
  onFilterChange() {
    this.list.page = 0;
    this.list.get();
  }

  // Xóa toàn bộ bộ lọc
  clearFilters() {
    this.filterText = '';
    this.filterStatus = null;
    this.filterAssignedUserId = null;
    this.sorting = ''; // Reset cả sắp xếp nếu muốn
    this.list.get();
  }

  loadProjects() {
    this.projectService.getList({ maxResultCount: 100, skipCount: 0 }).subscribe(res => {
      this.projects = res.items;
    });
  }

  // --- LOGIC FORM (CREATE / EDIT) ---

  buildForm() {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(256)]],
      description: [null],
      status: [TaskStatus.New, [Validators.required]],
      dueDate: [null],
      assignedUserIds: [[]],
      projectId: [null, [Validators.required]],
    });
    this.form.get('projectId').valueChanges.subscribe(() => {
      // Mỗi khi Dự án bị đổi, tự động xóa trắng ô chọn Người thực hiện
      this.form.get('assignedUserIds').setValue([]);
    });
  }

  createTask() {
    this.isEditMode = false;
    this.selectedTaskId = '';
    this.form.reset({ status: TaskStatus.New, projectId: this.filterProjectId });
    this.loadProjects();
    this.form.enable(); // Mở khóa tất cả input
    this.isModalOpen = true;
  }

  editTask(task: TaskDto) {
    if (this.projects.length === 0) this.loadProjects();
    if (this.users.length === 0) {
      this.taskService.getUserLookup().subscribe(result => {
        this.users = result.items;
      });
    }

    this.isEditMode = true;
    this.selectedTaskId = task.id;
    this.form.enable();

    // 2. Sử dụng patchValue với dữ liệu chuẩn hóa
    this.form.patchValue({
      ...task,
      dueDate: task.dueDate ? new Date(task.dueDate) : null,
      projectId: task.projectId,
      assignedUserIds: task.assignedUserIds || [],
    });

    // Logic phân quyền: Nếu là Assignee (nhưng ko phải Creator/Admin) -> Chỉ cho sửa Status
    const isCreator = task.creatorId === this.currentUserId;
    const isAssignee = task.assignedUserIds?.includes(this.currentUserId);

    if (isAssignee && !isCreator && !this.isAdmin) {
      this.form.get('title')?.disable();
      this.form.get('description')?.disable();
      this.form.get('assignedUserIds')?.disable();
      this.form.get('dueDate')?.disable();
      // Chỉ để lại 'status' là enable
    }

    this.isModalOpen = true;
  }

  handleCancel() {
    this.isModalOpen = false;
  }

  save() {
    if (this.form.invalid) return;

    this.saving = true;
    const formValue = this.form.getRawValue(); // getRawValue để lấy cả các trường bị disable

    const request = this.isEditMode
      ? this.taskService.update(this.selectedTaskId, formValue)
      : this.taskService.create(formValue);

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

  // --- LOGIC XÓA ---
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

  // --- HELPER LOGIC HIỂN THỊ ---

  canUpdate(task: TaskDto): boolean {
    return (
      this.isAdmin ||
      task.projectManagerId === this.currentUserId ||
      task.creatorId === this.currentUserId ||
      task.assignedUserIds?.includes(this.currentUserId)
    );
  }

  canDelete(task: TaskDto): boolean {
    return (
      this.isAdmin ||
      task.projectManagerId === this.currentUserId ||
      task.creatorId === this.currentUserId
    );
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

  get validAssignees(): UserLookupDto[] {
    // Lấy giá trị của ô Dự án đang được chọn trong Form
    const selectedProjectId = this.form?.get('projectId')?.value;

    if (!selectedProjectId || !this.projects) return [];

    // Tìm thông tin dự án đó trong mảng projects (mảng đang dùng để render dropdown Dự án)
    const selectedProject = this.projects.find(p => p.id === selectedProjectId);

    if (!selectedProject) return [];

    // Lọc ra những người là PM hoặc nằm trong MemberIds của dự án đó
    return this.users.filter(
      u =>
        u.id === selectedProject.managerId ||
        (selectedProject.memberIds && selectedProject.memberIds.includes(u.id)),
    );
  }
}
