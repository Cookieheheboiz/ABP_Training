// 1. 👇 Nhớ import thêm ChangeDetectorRef
import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  ElementRef,
  HostListener,
  NgZone,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { NotificationService, NotificationDto } from '@proxy/notifications';

import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { EnvironmentService } from '@abp/ng.core';
import { OAuthService } from 'angular-oauth2-oidc';
import { ToasterService } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './list.html', // Chú ý: Đuôi file html của bạn là .html hay .component.html thì giữ nguyên nhé
  styleUrls: ['./list.scss'],
})
export class ListComponent implements OnInit, OnDestroy {
  notifications: NotificationDto[] = [];
  unreadCount = 0;
  isOpen = false;

  private notificationService = inject(NotificationService);
  private router = inject(Router);
  private env = inject(EnvironmentService);
  private oauthService = inject(OAuthService);
  private toaster = inject(ToasterService);
  private eRef = inject(ElementRef);
  private zone = inject(NgZone);

  // 2. 👇 Inject cây gậy ép cập nhật giao diện
  private cdr = inject(ChangeDetectorRef);

  private hubConnection: HubConnection | undefined;

  toggleDropdown(event: Event) {
    event.preventDefault();
    this.isOpen = !this.isOpen;
  }

  @HostListener('document:click', ['$event'])
  clickout(event: Event) {
    if (!this.eRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }

  ngOnInit() {
    this.loadNotifications();
    this.initSignalR();
  }

  initSignalR() {
    const apiUrl = this.env.getApiUrl('default');

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(apiUrl + '/signalr-hubs/notification', {
        accessTokenFactory: () => this.oauthService.getAccessToken(),
      })
      .configureLogging(LogLevel.Information)
      .build();

    this.hubConnection.on('ReceiveNotification', (title: string, message: string) => {
      this.zone.run(() => {
        this.loadNotifications();
        this.toaster.info(message, title);

        // 3. 👇 Ép giao diện nảy số chuông ngay khi SignalR nhận tin nhắn
        this.cdr.detectChanges();
      });
    });

    this.hubConnection
      .start()
      .then(() => console.log('🟢 Đã kết nối SignalR thành công!'))
      .catch(err => console.error('🔴 Lỗi kết nối SignalR:', err));
  }

  ngOnDestroy() {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }

  loadNotifications() {
    this.notificationService.getMyNotifications().subscribe(res => {
      this.notifications = res;
      this.unreadCount = res.filter(n => !n.isRead).length;

      // 4. 👇 Ép giao diện vẽ lại ngay khi load xong dữ liệu (Trị dứt điểm lỗi F5 phải click mới hiện)
      this.cdr.detectChanges();
    });
  }

  onClickNotification(notification: NotificationDto) {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe(() => {
        notification.isRead = true;
        this.unreadCount = Math.max(0, this.unreadCount - 1);

        // 5. 👇 Ép vẽ lại chấm đỏ ngay khi click
        this.cdr.detectChanges();

        this.navigate(notification.targetUrl);
      });
    } else {
      this.navigate(notification.targetUrl);
    }
  }

  private navigate(url: string) {
    if (url) {
      this.router.navigateByUrl(url);
    }
  }

  markAllAsRead() {
    this.notificationService.markAllAsRead().subscribe(() => {
      this.notifications.forEach(n => (n.isRead = true));
      this.unreadCount = 0;

      // 6. 👇 Ép vẽ lại giao diện sau khi đọc tất cả
      this.cdr.detectChanges();
    });
  }
}
