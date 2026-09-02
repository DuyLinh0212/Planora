export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AuthenticationResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  userId: string;
  email: string;
  username: string;
  displayName: string;
  avatarUrl: string | null;
}

export interface PasswordResetResponse {
  message: string;
  resetToken: string | null;
  expiresAt: string | null;
}

export interface Project {
  id: string;
  name: string;
  description: string;
  /** API normally returns the enum name; number is accepted for backwards compatibility. */
  status: string | number;
  startAt: string | null;
  endAt: string | null;
  memberCount: number;
  updatedAt: string;
}

export interface ProjectActivity {
  id: string;
  action: string;
  entityType: string;
  entityId: string;
  actorUserId: string | null;
  actorDisplayName: string;
  createdAt: string;
}

export interface Sprint {
  id: string;
  projectId: string;
  name: string;
  goal: string | null;
  startAt: string;
  endAt: string;
  status: string;
}

export interface ProjectTask {
  id: string;
  projectId: string;
  sprintId: string | null;
  title: string;
  description: string;
  priority: string;
  status: string;
  originalDueAt: string | null;
  effectiveDueAt: string | null;
  acceptanceCriteria: string[];
  assigneeMemberIds: string[];
  type?: string;
  submissionRequirement?: string;
  allowedExtensions?: string[];
  dependsOnTaskId?: string | null;
  isMilestone?: boolean;
}

export interface ProjectMember {
  membershipId: string;
  userId: string;
  displayName: string;
  email: string;
  status: string;
  roles: string[];
}

export interface ProjectInvitation {
  id: string;
  email: string;
  status: string;
  expiresAt: string;
  createdAt: string;
  invitationToken?: string;
}

export interface RegisteredUserMatch {
  userId: string;
  displayName: string;
  email: string;
  avatarUrl: string | null;
  isAlreadyMember: boolean;
}

export interface ProjectRole {
  id: string;
  code: string;
  name: string;
  isSystemRole: boolean;
}

export interface ProjectCapabilities {
  permissionCodes: string[];
}

export interface ProjectRolePermissions extends ProjectRole {
  isEditable: boolean;
  permissionCodes: string[];
}

export interface ProjectFolder {
  id: string;
  projectId: string;
  parentFolderId: string | null;
  name: string;
}

export interface ProjectFile {
  id: string;
  projectId: string;
  folderId: string;
  name: string;
  mimeType: string;
  currentVersionId: string;
  versionNumber: number;
  sizeBytes: number;
  sourceTaskId?: string | null;
}

export interface ProjectDocument {
  id: string;
  projectId: string;
  folderId: string;
  title: string;
  currentVersionId: string;
  versionNumber: number;
  sourceTaskId?: string | null;
}

export interface DocumentVersionHistory {
  id: string;
  versionNumber: number;
  content: string;
  contentFormat: string;
  editedByUserId: string;
  editedByDisplayName: string;
  createdAt: string;
  changeNote: string | null;
}

export interface ProjectStorage {
  folders: ProjectFolder[];
  files: ProjectFile[];
  documents: ProjectDocument[];
}

export interface UsageQuota {
  planCode: string;
  planName: string;
  ownedProjects: number;
  maxOwnedProjects: number;
  storageBytes: number;
  maxStorageBytes: number;
  maxProjectStorageBytes: number;
  maxFileSizeBytes: number;
  dailyUploadBytes: number;
  dailyUploadCount: number;
  maxMembersPerProject: number;
  maxVersionsPerFile: number;
  subscriptionExpiresAt: string | null;
  autoRenew: boolean;
}

export interface GmailLinkResponse {
  isLinked: boolean;
  gmailAddress: string | null;
  isServerConfigured: boolean;
  lastSendFailedAt: string | null;
  lastSendFailureReason: string | null;
}

export interface UserProfile {
  userId: string;
  email: string;
  username: string;
  displayName: string;
  avatarUrl: string | null;
  preferredLanguage: 'vi' | 'en';
  themePreference: 'light' | 'dark' | 'calm';
  timeZoneId: string;
  emailTaskNotificationsEnabled: boolean;
  gmailLink: GmailLinkResponse;
  participatingProjectCount: number;
  quota: UsageQuota;
}

export interface UserNotification {
  id: string;
  type: string;
  title: string;
  message: string;
  entityType: string | null;
  entityId: string | null;
  createdAt: string;
  readAt: string | null;
  dismissedAt: string | null;
  isActionable: boolean;
}

export interface MaintenanceStatus {
  isEnabled: boolean;
  message: string;
  updatedAt: string | null;
}

export interface TaskExtensionRequest {
  id: string;
  taskId: string;
  requestedByUserId: string;
  requestedByDisplayName: string;
  requestedDueAt: string;
  reason: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  reviewedByUserId: string | null;
  reviewedAt: string | null;
  reviewNote: string | null;
  createdAt: string;
}

export interface TaskDeadlineChange {
  id: string;
  oldDueAt: string;
  newDueAt: string;
  changeType: string;
  countsAsLate: boolean;
  reason: string;
  changedByUserId: string;
  extensionRequestId: string | null;
  createdAt: string;
}

export interface UserPayment {
  id: string;
  planName: string;
  provider: string;
  amount: number;
  currency: string;
  status: string;
  createdAt: string;
  paidAt: string | null;
}

export interface BankTransferInstructions {
  bankName: string;
  accountName: string;
  accountNumber: string;
  transferContent: string;
  branch: string | null;
  qrCodeUrl: string;
}

export interface PaymentCheckout {
  payment: UserPayment;
  checkoutUrl: string | null;
  bankTransferInstructions: BankTransferInstructions | null;
}

export interface AvailablePlan {
  id: string;
  code: string;
  name: string;
  price: number;
  currency: string;
  billingPeriod: string;
  maxOwnedProjects: number;
  maxStorageBytes: number;
  entitlements: string[];
}

export interface SupportMessage {
  id: string;
  senderUserId: string;
  senderDisplayName: string;
  content: string;
  createdAt: string;
}

export interface SupportConversation {
  id: string;
  kind: string;
  subject: string;
  status: string;
  paymentTransactionId: string | null;
  createdAt: string;
  closedAt: string | null;
  messages: SupportMessage[];
}

export interface ProjectDraft {
  name: string;
  description: string;
  startAt: string | null;
  endAt: string | null;
  status?: string | number;
}

export interface SprintDraft {
  name: string;
  goal: string | null;
  startAt: string;
  endAt: string;
}

export interface TaskDraft {
  sprintId: string | null;
  title: string;
  description: string;
  priority: string;
  dueAt: string | null;
  acceptanceCriteria: string[];
  type?: string;
  submissionRequirement?: string;
  allowedExtensions?: string[];
  dependsOnTaskId?: string | null;
  isMilestone?: boolean;
}

export interface TaskSubmissionLinkDraft {
  url: string;
  linkType: string;
  title: string | null;
}

export interface TaskSubmissionResponse {
  id: string;
  taskId: string;
  attemptNumber: number;
  submittedAt: string;
  status: string;
}

export interface TaskSubmissionDetail extends TaskSubmissionResponse {
  description: string | null;
  submittedByUserId: string;
  submittedByDisplayName: string;
  links: { id: string; url: string; linkType: string; title: string | null }[];
  files: {
    projectFileId: string;
    fileVersionId: string;
    name: string;
    mimeType: string;
    sizeBytes: number;
  }[];
  reviewFeedback?: string | null;
  reviewedAt?: string | null;
}
