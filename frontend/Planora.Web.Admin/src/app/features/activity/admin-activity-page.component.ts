import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { AdminConsoleContextService } from '../../core/admin-console-context.service';
import { PlanoraAdminApiService } from '../../core/planora-admin-api.service';

@Component({
  selector: 'app-admin-activity-page',
  imports: [DatePipe],
  templateUrl: './admin-activity-page.component.html',
  styleUrl: './admin-activity-page.component.css',
})
export class AdminActivityPageComponent {
  readonly context = inject(AdminConsoleContextService);
  private readonly api = inject(PlanoraAdminApiService);
  reloadActivity(): void {
    this.api.getAdminActivity().subscribe((response) => this.context.activity.set(response.items));
  }
}
