import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AdminRealtimeService } from './core/admin-realtime.service';
@Component({ selector: 'app-root', imports: [RouterOutlet], templateUrl: './app.html',
  styleUrl: './app.css', })
export class App { constructor() { void inject(AdminRealtimeService).start(); } }
