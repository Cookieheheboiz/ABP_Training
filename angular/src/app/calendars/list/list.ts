import { Component, inject, NgZone, ChangeDetectorRef } from '@angular/core';
import { CalendarOptions } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import listPlugin from '@fullcalendar/list';
import interactionPlugin from '@fullcalendar/interaction';

// Import service từ proxy calendars mới tạo
import { CalendarService } from '@proxy/calendars';
import { TaskStatus } from '@proxy/tasks';
import { LocalizationService } from '@abp/ng.core';

@Component({
  selector: 'app-list',
  templateUrl: './list.html',
  styleUrls: ['./list.scss'],
  // eslint-disable-next-line @angular-eslint/prefer-standalone
  standalone: false,
})
export class List {
  isTaskModalVisible = false;
  selectedTask: any = null;

  private calendarService = inject(CalendarService);
  private zone = inject(NgZone);
  private cdr = inject(ChangeDetectorRef);
  private localization = inject(LocalizationService);
  today = new Date();

  calendarOptions: CalendarOptions = {
    plugins: [dayGridPlugin, timeGridPlugin, listPlugin, interactionPlugin],
    initialView: 'dayGridMonth',
    headerToolbar: {
      left: 'prev,next today',
      center: 'title',
      right: 'dayGridMonth,timeGridWeek,listWeek',
    },
    buttonText: {
      today: this.localization.instant('::Calendar:Today'),
      month: this.localization.instant('::Calendar:Month'),
      week: this.localization.instant('::Calendar:Week'),
      list: this.localization.instant('::Calendar:List'),
    },
    buttonHints: {
      today: this.localization.instant('::Calendar:Hint:Today'),
      prev: this.localization.instant('::Calendar:Hint:Prev'),
      next: this.localization.instant('::Calendar:Hint:Next'),
      dayGridMonth: this.localization.instant('::Calendar:Hint:MonthView'),
      timeGridWeek: this.localization.instant('::Calendar:Hint:WeekView'),
      listWeek: this.localization.instant('::Calendar:Hint:ListView'),
    },

    dayMaxEvents: 3,
    moreLinkContent: args => {
      return `+${args.num} ${this.localization.instant('::Calendar:MoreTasks')}`;
    },
    moreLinkClick: 'popover',
    defaultTimedEventDuration: '01:00:00', // Ép mọi task có DueDate đều dài đúng 1 tiếng trên giao diện
    slotEventOverlap: true, // Cho phép các task trùng giờ xếp chồng lên nhau (Cascade)
    eventMinHeight: 40, // Ép chiều cao tối thiểu là 40px để khối to ra, dễ click và hiện đủ chữ
    slotMinTime: '06:00:00', // Cắt bỏ khoảng thời gian rạng sáng để khung lịch thoáng hơn
    slotMaxTime: '23:00:00',

    eventDisplay: 'block',
    eventMouseEnter: info => {
      info.el.setAttribute('title', info.event.title);
      info.el.style.cursor = 'pointer';
      info.el.style.opacity = '0.8';
    },
    eventMouseLeave: info => {
      info.el.style.opacity = '1';
    },

    // 2. HIỆU ỨNG CLICK: Mở Modal chi tiết
    eventClick: info => {
      this.zone.run(() => {
        this.selectedTask = info.event.extendedProps;
        this.isTaskModalVisible = true;
      });
    },

    eventContent: arg => {
      const task = arg.event.extendedProps;
      const now = new Date().getTime();
      const taskTime = task.dueDate ? new Date(task.dueDate).getTime() : 0;
      const isOverdue = taskTime > 0 && taskTime < now && task.status !== TaskStatus.Completed;

      const warningIcon = isOverdue
        ? `<i class="fa fa-exclamation-circle text-danger me-1 bg-white rounded-circle" style="padding: 1px;"></i>`
        : '';

      if (arg.view.type === 'listWeek') {
        return {
          html: `<div style="font-weight: 500; color: #212529;">
                  ${warningIcon} ${arg.event.title}
                 </div>`,
        };
      }

      return {
        html: `
          <div class="px-1 text-truncate text-white" style="font-size: 12px; font-weight: 500;">
            ${warningIcon}
            <span class="opacity-75 me-1">${arg.timeText}</span> 
            ${arg.event.title}
          </div>
        `,
      };
    },

    events: (info, successCallback, failureCallback) => {
      this.calendarService
        .getCalendarTasks({
          projectId: undefined,
          startDate: info.start.toISOString(),
          endDate: info.end.toISOString(),
        })
        .subscribe({
          next: tasks => {
            const events = tasks.map(task => ({
              id: task.id,
              title: task.projectName ? `[${task.projectName}] ${task.title}` : task.title,
              start: task.dueDate,
              allDay: false,
              backgroundColor: this.getStatusColor(task.status), // Lấy màu chuẩn theo trạng thái
              borderColor: 'transparent',
              extendedProps: { ...task }, // Lưu lại để dùng cho Modal
            }));
            successCallback(events);
          },
          error: err => failureCallback(err),
        });
    },
  };

  // Đồng bộ màu sắc với các màn hình khác
  getStatusColor(status: TaskStatus): string {
    switch (status) {
      case TaskStatus.New:
        return '#1890ff';
      case TaskStatus.InProgress:
        return '#fa8c16';
      case TaskStatus.Completed:
        return '#52c41a';
      default:
        return '#d9d9d9';
    }
  }

  getStatusText(status: TaskStatus): string {
    switch (status) {
      case TaskStatus.New:
        return this.localization.instant('::TaskStatus:New');
      case TaskStatus.InProgress:
        return this.localization.instant('::TaskStatus:InProgress');
      case TaskStatus.Completed:
        return this.localization.instant('::TaskStatus:Completed');
      default:
        return this.localization.instant('::TaskStatus:Unknown');
    }
  }
}
