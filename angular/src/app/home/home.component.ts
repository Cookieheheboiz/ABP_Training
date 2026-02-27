import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ConfigStateService, CurrentUserDto } from '@abp/ng.core';
import { CoreModule } from '@abp/ng.core';

@Component({
  selector: 'app-home',
  standalone: true,
  // Đã thêm CommonModule và RouterModule để HTML không bị lỗi, đồng thời bỏ LocalizationPipe thừa
  imports: [CommonModule, RouterModule, CoreModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent {
  private configState = inject(ConfigStateService);

  // Khai báo Getter lấy dữ liệu User trực tiếp từ hệ thống lõi của ABP
  get currentUser(): CurrentUserDto {
    return this.configState.getOne('currentUser');
  }
}
