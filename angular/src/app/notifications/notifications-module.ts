import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ListComponent } from './list/list';

@NgModule({
  declarations: [],

  imports: [CommonModule, ListComponent],
  exports: [ListComponent],
})
export class NotificationsModule {}
