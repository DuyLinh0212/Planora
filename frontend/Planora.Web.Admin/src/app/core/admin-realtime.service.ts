import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminRealtimeService {
  private connection: HubConnection | null = null;
  private readonly supportGroups = new Set<string>();
  readonly connected = signal(false);
  readonly supportMessageVersion = signal(0);

  async start(): Promise<void> {
    const token = localStorage.getItem('planora.admin.accessToken');
    if (!token || localStorage.getItem('planora.admin.preview') === 'true') return;
    if (!this.connection) this.connection = this.createConnection();
    if (this.connection.state !== HubConnectionState.Disconnected) return;
    try { await this.connection.start(); this.connected.set(true); await this.restoreGroups(); } catch { this.connected.set(false); }
  }

  async joinSupport(conversationId: string): Promise<void> {
    this.supportGroups.add(conversationId);
    await this.start();
    if (this.connection?.state === HubConnectionState.Connected) await this.connection.invoke('JoinSupportGroup', conversationId);
  }

  private createConnection(): HubConnection {
    const connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/planora`, { accessTokenFactory: () => localStorage.getItem('planora.admin.accessToken') ?? '' })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build();
    connection.on('SupportMessageReceived', () => this.supportMessageVersion.update((value) => value + 1));
    connection.onreconnected(() => { this.connected.set(true); void this.restoreGroups(); });
    connection.onreconnecting(() => this.connected.set(false));
    connection.onclose(() => this.connected.set(false));
    return connection;
  }

  private async restoreGroups(): Promise<void> { for (const id of this.supportGroups) await this.connection?.invoke('JoinSupportGroup', id); }
}
