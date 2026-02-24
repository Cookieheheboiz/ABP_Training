import { Component, OnInit, inject } from '@angular/core';
import { PagedResultDto } from '@abp/ng.core';
import { ProjectService, ProjectDto, CreateUpdateProjectDto } from '@proxy/projects';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { ConfigStateService } from '@abp/ng.core';

@Component({
  selector: 'app-project-list',
  templateUrl: './list.html',
  styleUrls: ['./list.scss'],
  // eslint-disable-next-line @angular-eslint/prefer-standalone
  standalone: false,
})
export class ProjectListComponent implements OnInit {
  projectData: PagedResultDto<ProjectDto> = { items: [], totalCount: 0 };
  loading = false;

  // Modal & Form State
  isModalOpen = false;
  isEditMode = false;
  saving = false;
  selectedProjectId = '';
  form: FormGroup;
  filterText = '';

  // Inject Services
  private readonly projectService = inject(ProjectService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly message = inject(NzMessageService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly config = inject(ConfigStateService);

  users: any[] = [];

  ngOnInit(): void {
    this.buildForm();
    this.loadData();
    this.loadUsers();
  }

  buildForm() {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(128)]],
      description: ['', [Validators.maxLength(500)]],
      managerId: [null, [Validators.required]],
      memberIds: [[]],
    });
  }

  loadData() {
    this.loading = true;
    this.projectService
      .getList({
        filterText: this.filterText,
        maxResultCount: 100,
        skipCount: 0,
      })
      .subscribe({
        next: res => {
          this.projectData = res;
          this.loading = false;
        },
        error: () => (this.loading = false),
      });
  }

  loadUsers() {
    // Gọi hàm lookup bạn đã viết ở ProjectAppService
    this.projectService.getUserLookup().subscribe(res => {
      this.users = res.items;
    });
  }

  // --- ACTIONS ---

  createProject() {
    this.isEditMode = false;
    this.selectedProjectId = '';
    this.form.reset();
    this.isModalOpen = true;
  }

  editProject(project: ProjectDto, event: MouseEvent) {
    event.stopPropagation(); // Chặn click xuyên thấu
    this.isEditMode = true;
    this.selectedProjectId = project.id;
    this.form.patchValue({
      name: project.name,
      description: project.description,
      managerId: project.managerId,
      memberIds: project.memberIds || [],
    });
    this.isModalOpen = true;
  }

  save() {
    if (this.form.invalid) {
      // Đánh dấu các trường là dirty để hiển thị lỗi
      Object.values(this.form.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    this.saving = true;
    const input: CreateUpdateProjectDto = this.form.value;

    const request = this.isEditMode
      ? this.projectService.update(this.selectedProjectId, input)
      : this.projectService.create(input);

    request.subscribe({
      next: () => {
        this.message.success(this.isEditMode ? 'Cập nhật thành công!' : 'Tạo mới thành công!');
        this.isModalOpen = false;
        this.loadData();
        this.saving = false;
      },
      error: () => (this.saving = false),
    });
  }

  deleteProject(id: string, event: MouseEvent) {
    event.stopPropagation();
    this.confirmation.warn('::AreYouSureToDelete', '::ConfirmDelete').subscribe(status => {
      if (status === Confirmation.Status.confirm) {
        this.projectService.delete(id).subscribe(() => {
          this.message.success('::Successfully Deleted');
          this.loadData();
        });
      }
    });
  }

  // --- NAVIGATION & UI HELPER ---

  goToProjectDetail(id: string) {
    this.router.navigate(['/projects', id]);
  }

  getAvatarColor(name: string): string {
    const colors = ['#f56a00', '#7265e6', '#ffbf00', '#00a2ae', '#1890ff', '#52c41a'];
    let hash = 0;
    for (let i = 0; i < name?.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash);
    return colors[Math.abs(hash % colors.length)];
  }

  get currentUserId(): string {
    return this.config.getOne('currentUser')?.id;
  }

  get isAdmin(): boolean {
    return this.config.getOne('currentUser')?.roles.includes('admin');
  }

  canEditOrDelete(project: ProjectDto): boolean {
    return this.isAdmin || project.managerId === this.currentUserId;
  }
}
