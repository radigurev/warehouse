export interface AuditLogDto {
  id: number;
  userId: number | null;
  action: string;
  resource: string;
  details: string | null;
  ipAddress: string | null;
  createdAt: string;
}
