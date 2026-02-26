import { Component, inject, NgZone, ChangeDetectorRef } from '@angular/core';
import { CalendarOptions } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import listPlugin from '@fullcalendar/list';
import interactionPlugin from '@fullcalendar/interaction';

// Import service từ proxy calendars mới tạo
import { CalendarService } from '@proxy/calendars';
import { TaskStatus } from '@proxy/tasks';

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
      today: 'Hôm nay',
      month: 'Tháng',
      week: 'Tuần',
      list: 'Danh sách',
    },

    dayMaxEvents: 3, // Giới hạn tối đa 3 task trên 1 ô ngày
    moreLinkContent: args => {
      // Việt hóa nút "+X more" mặc định của tiếng Anh
      return `+${args.num} công việc nữa`;
    },
    moreLinkClick: 'popover',
    defaultTimedEventDuration: '01:00:00', // Ép mọi task có DueDate đều dài đúng 1 tiếng trên giao diện
    slotEventOverlap: true, // Cho phép các task trùng giờ xếp chồng lên nhau (Cascade)
    eventMinHeight: 40, // Ép chiều cao tối thiểu là 40px để khối to ra, dễ click và hiện đủ chữ
    slotMinTime: '06:00:00', // Cắt bỏ khoảng thời gian rạng sáng để khung lịch thoáng hơn
    slotMaxTime: '23:00:00',

    eventDisplay: 'block',
    // Tự động gọi API mỗi khi người dùng chuyển tháng/tuần
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
        // In thử data ra F12 để Huy yên tâm là FullCalendar có truyền dữ liệu
        console.log('Dữ liệu Task vừa click:', info.event.extendedProps);

        // Gán trực tiếp data và mở Modal
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

      // Nếu đang ở màn hình Danh sách (List View)
      if (arg.view.type === 'listWeek') {
        return {
          html: `<div style="font-weight: 500; color: #212529;">
                  ${warningIcon} ${arg.event.title}
                 </div>`,
        };
      }

      // Nếu ở màn hình Tháng/Tuần (Grid View - nền màu đậm)
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
        return 'Mới tạo';
      case TaskStatus.InProgress:
        return 'Đang thực hiện';
      case TaskStatus.Completed:
        return 'Đã hoàn thành';
      default:
        return 'Không xác định';
    }
  }
}
