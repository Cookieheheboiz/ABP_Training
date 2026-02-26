import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { List } from './list/list';
import { FullCalendarModule } from '@fullcalendar/angular';
import { ThemeSharedModule } from '@abp/ng.theme.shared';
import { RouterModule, Routes } from '@angular/router';
import { CoreModule } from '@abp/ng.core';

import { NzTableModule } from 'ng-zorro-antd/table';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzStatisticModule } from 'ng-zorro-antd/statistic';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
const routes: Routes = [{ path: '', component: List }];

@NgModule({
  declarations: [List],
  imports: [
    CommonModule,
    CoreModule,
    FullCalendarModule,
    ThemeSharedModule,
    RouterModule.forChild(routes),
    NzCardModule,
    NzButtonModule,
    NzIconModule,
    NzModalModule,
    NzFormModule,
    NzInputModule,
    NzSpinModule,
    NzAvatarModule,
    NzEmptyModule,
    NzDropDownModule,
    NzGridModule,
    NzSelectModule,
    NzStatisticModule,
    NzProgressModule,
    NzDrawerModule,
    NzTagModule,
    NzBadgeModule,
    NzToolTipModule,
    NzTableModule,
    NzDatePickerModule,
    NzListModule,
    NzPopconfirmModule,
  ],
})
export class CalendarsModule {}
