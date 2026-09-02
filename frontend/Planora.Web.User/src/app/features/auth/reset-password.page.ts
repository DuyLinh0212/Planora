import { Component } from '@angular/core';
import { ForgotPasswordPage } from './forgot-password.page';

@Component({
  selector: 'app-reset-password-page',
  standalone: true,
  imports: [ForgotPasswordPage],
  templateUrl: './reset-password.page.html',
  styleUrl: './reset-password.page.css',
})
export class ResetPasswordPage {}
