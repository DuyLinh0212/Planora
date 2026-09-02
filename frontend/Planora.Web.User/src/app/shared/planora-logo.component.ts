import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-planora-logo',
  templateUrl: './planora-logo.component.html',
  styleUrl: './planora-logo.component.css',
})
export class PlanoraLogoComponent {
  @Input() tone: 'light' | 'dark' = 'light';
  @Input() compact = false;
}
