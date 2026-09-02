import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

type GoogleCredentialResponse = { credential?: string };
type GooglePromptMoment = { isNotDisplayed: () => boolean; isSkippedMoment: () => boolean };
type GoogleCodeResponse = { code?: string; error?: string; error_description?: string };
type GoogleCodeClient = { requestCode: () => void };

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (options: {
            client_id: string;
            callback: (response: GoogleCredentialResponse) => void;
            cancel_on_tap_outside?: boolean;
            use_fedcm_for_prompt?: boolean;
          }) => void;
        };
        prompt: (callback?: (notification: GooglePromptMoment) => void) => void;
      };
        oauth2: {
          initCodeClient: (options: {
            client_id: string;
            scope: string;
            ux_mode: 'popup';
            callback: (response: GoogleCodeResponse) => void;
            error_callback?: (error: { type?: string }) => void;
            include_granted_scopes?: boolean;
            login_hint?: string;
            select_account?: boolean;
          }) => GoogleCodeClient;
        };
      };
    };
  }
}

@Injectable({ providedIn: 'root' })
export class GoogleIdentityService {
  private loader: Promise<void> | null = null;

  async requestIdToken(): Promise<string> {
    if (!environment.googleClientId) {
      throw new Error('Google Login chưa được cấu hình Client ID trong environment.');
    }
    await this.loadSdk();
    return new Promise<string>((resolve, reject) => {
      window.google!.accounts.id.initialize({
        client_id: environment.googleClientId,
        cancel_on_tap_outside: true,
        // Keep the explicit login button usable when Chrome has disabled
        // FedCM for this origin (for example after a previous dismissal).
        // Google Identity Services will use its regular prompt instead.
        use_fedcm_for_prompt: false,
        callback: (response) => {
          if (response.credential) resolve(response.credential);
          else reject(new Error('Google không trả về identity token.'));
        },
      });
      window.google!.accounts.id.prompt((notification) => {
        if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
          reject(new Error('Google Login đang bị trình duyệt chặn. Hãy kiểm tra Allowed JavaScript origins hoặc cho phép đăng nhập bên thứ ba cho trang này.'));
        }
      });
    });
  }

  async requestGmailAuthorizationCode(loginHint: string): Promise<{ code: string; redirectUri: string }> {
    const clientId = environment.gmailOAuthClientId || environment.googleClientId;
    if (!clientId) {
      throw new Error('Liên kết Gmail chưa được cấu hình Client ID trong environment.');
    }

    await this.loadSdk();
    const redirectUri = window.location.origin;
    return new Promise((resolve, reject) => {
      const client = window.google!.accounts.oauth2.initCodeClient({
        client_id: clientId,
        scope: 'openid email https://www.googleapis.com/auth/gmail.send',
        ux_mode: 'popup',
        include_granted_scopes: true,
        login_hint: loginHint,
        select_account: true,
        callback: (response) => {
          if (response.code) resolve({ code: response.code, redirectUri });
          else reject(new Error(response.error_description || 'Google không trả về mã liên kết Gmail.'));
        },
        error_callback: (error) => {
          const message = error.type === 'popup_closed'
            ? 'Bạn đã đóng cửa sổ liên kết Gmail.'
            : 'Không thể mở cửa sổ liên kết Gmail.';
          reject(new Error(message));
        },
      });
      client.requestCode();
    });
  }

  private loadSdk(): Promise<void> {
    if (window.google?.accounts?.id) return Promise.resolve();
    if (this.loader) return this.loader;
    this.loader = new Promise<void>((resolve, reject) => {
      const existing = document.querySelector<HTMLScriptElement>('script[data-planora-google]');
      if (existing) {
        existing.addEventListener('load', () => resolve(), { once: true });
        existing.addEventListener('error', () => reject(new Error('Không tải được Google SDK.')), {
          once: true,
        });
        return;
      }
      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.dataset['planoraGoogle'] = 'true';
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('Không tải được Google SDK.'));
      document.head.appendChild(script);
    });
    return this.loader;
  }
}
