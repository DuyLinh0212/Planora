import { Injectable, effect, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { PlanoraApiService } from '../api/planora-api.service';
import { accessToken } from '../auth/session.store';
import { WorkspaceStore } from '../workspace/workspace.store';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RealtimeNotificationService {
  private readonly store = inject(WorkspaceStore);
  private readonly api = inject(PlanoraApiService);
  private connection: HubConnection | null = null;
  private joinedProjectId = '';
  private joinedSupportIds = new Set<string>();
  readonly connected = signal(false);
  readonly supportMessageVersion = signal(0);

  constructor() {
    effect(() => {
      const projectId = this.store.project().id;
      if (!this.connection || this.connection.state !== HubConnectionState.Connected || projectId === this.joinedProjectId) return;
      void this.changeProjectGroup(projectId);
    });
  }

  async start(): Promise<void> {
    if (!accessToken() || this.connection?.state === HubConnectionState.Connected || this.connection?.state === HubConnectionState.Connecting) return;
    if (!this.connection) this.connection = this.createConnection();
    try {
      await this.connection.start();
      this.connected.set(true);
      if (this.store.project().id) await this.changeProjectGroup(this.store.project().id);
      for (const id of this.joinedSupportIds) await this.connection.invoke('JoinSupportGroup', id);
    } catch {
      this.connected.set(false);
    }
  }

  async joinSupport(conversationId: string): Promise<void> {
    this.joinedSupportIds.add(conversationId);
    await this.start();
    if (this.connection?.state === HubConnectionState.Connected)
      await this.connection.invoke('JoinSupportGroup', conversationId);
  }

  private createConnection(): HubConnection {
    const connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/planora`, { accessTokenFactory: () => accessToken() ?? '' })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build();

    const refreshNotifications = () => this.api.getNotifications().subscribe((items) => this.store.notifications.set(items));
    const refreshTasks = () => this.store.reloadTasks();
    connection.on('NotificationReceived', refreshNotifications);
    connection.on('ProjectInvitationReceived', refreshNotifications);
    connection.on('TaskAssigned', () => { refreshNotifications(); refreshTasks(); });
    connection.on('TaskSubmitted', refreshTasks);
    connection.on('TaskReworkRequested', refreshTasks);
    connection.on('SupportMessageReceived', (message) => {
      this.supportMessageVersion.update((value) => value + 1);
      globalThis.dispatchEvent(new CustomEvent('planora:support-message', { detail: message }));
      refreshNotifications();
    });
    connection.onreconnected(() => { this.connected.set(true); void this.restoreGroups(); });
    connection.onreconnecting(() => this.connected.set(false));
    connection.onclose(() => this.connected.set(false));
    return connection;
  }

  private async changeProjectGroup(projectId: string): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) return;
    if (this.joinedProjectId) await this.connection.invoke('LeaveProjectGroup', this.joinedProjectId);
    this.joinedProjectId = projectId;
    if (projectId) await this.connection.invoke('JoinProjectGroup', projectId);
  }

  private async restoreGroups(): Promise<void> {
    if (this.store.project().id) {
      this.joinedProjectId = '';
      await this.changeProjectGroup(this.store.project().id);
    }
    for (const id of this.joinedSupportIds) await this.connection?.invoke('JoinSupportGroup', id);
  }
}
