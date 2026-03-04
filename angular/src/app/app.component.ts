// 1. Nhớ import thêm hàm inject từ @angular/core
import { Component, inject } from '@angular/core';
import { DynamicLayoutComponent } from '@abp/ng.core';
import { LoaderBarComponent, NavItemsService } from '@abp/ng.theme.shared';
import { ListComponent } from './notifications/list/list';

@Component({
  selector: 'app-root',
  template: `
    <abp-loader-bar />
    <abp-dynamic-layout />
  `,
  imports: [LoaderBarComponent, DynamicLayoutComponent],
})
export class AppComponent {
  private navItems = inject(NavItemsService);
  constructor() {
    this.navItems.addItems([
      {
        id: 'NotificationBell',
        component: ListComponent,
        order: 100,
      },
    ]);
  }
}
