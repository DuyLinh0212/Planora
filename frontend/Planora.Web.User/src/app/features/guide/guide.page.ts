import { Component, ElementRef, ViewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideArrowRight,
  LucideBookOpenText,
  LucideBriefcaseBusiness,
  LucideCalendarRange,
  LucideCaptions,
  LucideCircleCheckBig,
  LucideListChecks,
  LucidePlay,
  LucideVolume2,
} from '@lucide/angular';

@Component({
  selector: 'app-guide-page',
  imports: [
    RouterLink,
    LucideArrowRight,
    LucideBookOpenText,
    LucideBriefcaseBusiness,
    LucideCalendarRange,
    LucideCaptions,
    LucideCircleCheckBig,
    LucideListChecks,
    LucidePlay,
    LucideVolume2,
  ],
  templateUrl: './guide.page.html',
  styleUrl: './guide.page.css',
})
export class GuidePage {
  @ViewChild('tutorialVideo') private readonly tutorialVideo?: ElementRef<HTMLVideoElement>;

  readonly chapters = [
    { label: '00:00', time: 0, title: 'Tổng quan', description: 'Luồng làm việc trong một phút' },
    { label: '00:13', time: 13.44, title: 'Tạo project', description: 'Phạm vi, mô tả và thời gian' },
    { label: '00:29', time: 28.92, title: 'Lập sprint', description: 'Mục tiêu cho một nhịp làm việc' },
    { label: '00:43', time: 43.22, title: 'Giao công việc', description: 'Deadline và tiêu chí hoàn thành' },
    { label: '01:01', time: 60.52, title: 'Nộp và duyệt', description: 'Hoàn tất hoặc yêu cầu làm lại' },
  ];

  seekTo(time: number): void {
    const video = this.tutorialVideo?.nativeElement;
    if (!video) return;
    video.currentTime = time;
    void video.play();
  }
}
