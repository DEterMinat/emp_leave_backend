# NoSQL (MongoDB) Query Inventory

เอกสารนี้สรุปว่าในโปรเจกต์มีโค้ด NoSQL query ตรงไหน และโค้ดแต่ละส่วนใช้ทำอะไร
อัปเดตล่าสุด: 2026-03-30

## 1) โครงสร้างการเชื่อมต่อ MongoDB

ไฟล์: `src/EmployeeLeaveApi/Data/MongoDbContext.cs`

```csharp
public IMongoCollection<User> Users => _database.GetCollection<User>("users");
public IMongoCollection<Role> Roles => _database.GetCollection<Role>("roles");
public IMongoCollection<Department> Departments => _database.GetCollection<Department>("departments");
public IMongoCollection<Employee> Employees => _database.GetCollection<Employee>("employees");
public IMongoCollection<LeaveType> LeaveTypes => _database.GetCollection<LeaveType>("leaveTypes");
public IMongoCollection<LeaveRequest> LeaveRequests => _database.GetCollection<LeaveRequest>("leaveRequests");
public IMongoCollection<LeaveBalance> LeaveBalances => _database.GetCollection<LeaveBalance>("leaveBalances");
public IMongoCollection<LeaveAttachment> LeaveAttachments => _database.GetCollection<LeaveAttachment>("leaveAttachments");
public IMongoCollection<DeviceToken> DeviceTokens => _database.GetCollection<DeviceToken>("deviceTokens");
public IMongoCollection<ActivityLog> ActivityLogs => _database.GetCollection<ActivityLog>("activityLogs");
public IMongoCollection<Attendance> Attendances => _database.GetCollection<Attendance>("attendances");
public IMongoCollection<UserNotification> UserNotifications => _database.GetCollection<UserNotification>("userNotifications");
```

ใช้ทำอะไร:
- กำหนด mapping ระหว่าง model ใน C# กับ collection ใน MongoDB
- เป็นศูนย์กลางที่ controller/service ทั้งระบบเรียกใช้งาน query

---

## 2) รายชื่อไฟล์ที่มี MongoDB Query จริง

ไฟล์ที่มีการเรียกเมทอดเช่น `Find`, `InsertOneAsync`, `UpdateOneAsync`, `DeleteOneAsync`, `CountDocumentsAsync`, `FindAsync`:

1. `src/EmployeeLeaveApi/Controllers/AuthController.cs`
2. `src/EmployeeLeaveApi/Controllers/DatabaseCheckController.cs`
3. `src/EmployeeLeaveApi/Controllers/DeviceTokensController.cs`
4. `src/EmployeeLeaveApi/Controllers/EmployeesController.cs`
5. `src/EmployeeLeaveApi/Controllers/LeaveBalancesController.cs`
6. `src/EmployeeLeaveApi/Controllers/ReferenceControllers.cs`
7. `src/EmployeeLeaveApi/Controllers/SeederController.cs`
8. `src/EmployeeLeaveApi/Diagnostics.cs`
9. `src/EmployeeLeaveApi/Services/ActivityLogService.cs`
10. `src/EmployeeLeaveApi/Services/AttendanceService.cs`
11. `src/EmployeeLeaveApi/Services/LeaveService.cs`
12. `src/EmployeeLeaveApi/Services/NotificationService.cs`
13. `src/EmployeeLeaveApi/Services/UserService.cs`

---

## 3) ตัวอย่างโค้ดสำคัญ + ความหมาย

### 3.1 Login/Register/Change Password
ไฟล์: `src/EmployeeLeaveApi/Controllers/AuthController.cs`

```csharp
var user = await _context.Users.Find(u => u.Username == request.Username).FirstOrDefaultAsync();
var role = await _context.Roles.Find(r => r.Id == user.RoleId).FirstOrDefaultAsync();
```

ใช้ทำอะไร:
- ค้นหา user สำหรับ login
- ดึง role ของ user เพื่อสร้าง token และกำหนดสิทธิ์

```csharp
var existing = await _context.Users.Find(u => u.Username == request.Username).FirstOrDefaultAsync();
await _context.Roles.InsertOneAsync(newRole);
```

ใช้ทำอะไร:
- เช็ค username ซ้ำก่อนสมัคร
- สร้าง role ใหม่อัตโนมัติในบางกรณี (เช่น Admin/Manager/HR/Employee)

```csharp
var update = Builders<User>.Update
    .Set(u => u.Password, hashedPassword)
    .Set(u => u.UpdatedAt, DateTime.UtcNow);

await _context.Users.UpdateOneAsync(u => u.Id == request.UserId, update);
```

ใช้ทำอะไร:
- เปลี่ยนรหัสผ่านผู้ใช้ (update เอกสารใน collection `users`)

### 3.2 Employee CRUD
ไฟล์: `src/EmployeeLeaveApi/Controllers/EmployeesController.cs`

```csharp
var employees = await _context.Employees.Find(_ => true).ToListAsync();
var dept = await _context.Departments.Find(d => d.Id == e.DepartmentId).FirstOrDefaultAsync();
var user = await _context.Users.Find(u => u.Id == e.UserId).FirstOrDefaultAsync();
```

ใช้ทำอะไร:
- ดึงรายการพนักงานทั้งหมด
- join แบบแอปพลิเคชัน (manual lookup) ไปที่ departments/users เพื่อประกอบ DTO

```csharp
await _context.Employees.InsertOneAsync(employee);
var result = await _context.Employees.UpdateOneAsync(e => e.Id == id, update);
var result = await _context.Employees.DeleteOneAsync(e => e.Id == id);
```

ใช้ทำอะไร:
- Create, Update, Delete ข้อมูลพนักงาน

### 3.3 Leave Request Workflow
ไฟล์: `src/EmployeeLeaveApi/Services/LeaveService.cs`

```csharp
await _context.LeaveRequests.InsertOneAsync(request);
await _context.LeaveAttachments.InsertOneAsync(attachment);
```

ใช้ทำอะไร:
- สร้างคำขอลา
- บันทึกไฟล์แนบของคำขอลา

```csharp
var request = await _context.LeaveRequests.Find(r => r.Id == id).FirstOrDefaultAsync();
var cursor = await _context.LeaveBalances.FindAsync(
    b => b.EmployeeId == request.EmployeeId && b.LeaveTypeId == request.LeaveTypeId && b.Year == year
);
var balance = await cursor.FirstOrDefaultAsync();
```

ใช้ทำอะไร:
- อ่านคำขอลาและยอดวันลาคงเหลือ ก่อนอนุมัติ

```csharp
var result = await _context.LeaveRequests.UpdateOneAsync(r => r.Id == id, update);
await _context.LeaveBalances.UpdateOneAsync(b => b.Id == balance.Id, balanceUpdate);
var result = await _context.LeaveRequests.DeleteOneAsync(r => r.Id == id);
```

ใช้ทำอะไร:
- อนุมัติ/ปฏิเสธคำขอลา (แก้สถานะ)
- ปรับยอดวันลา
- ลบคำขอลา

### 3.4 Notification + Device Token
ไฟล์: `src/EmployeeLeaveApi/Services/NotificationService.cs`

```csharp
await _context.UserNotifications.InsertOneAsync(notification);
var tokens = await _context.DeviceTokens.Find(t => t.UserId == userId).ToListAsync();
await _context.DeviceTokens.DeleteOneAsync(t => t.Id == deviceToken.Id);
var result = await _context.UserNotifications.UpdateOneAsync(n => n.Id == notificationId, update);
```

ใช้ทำอะไร:
- บันทึกการแจ้งเตือนลงฐานข้อมูล
- ดึง device token สำหรับส่ง push notification
- ลบ token ที่หมดอายุ/ใช้ไม่ได้
- mark notification เป็นอ่านแล้ว

ไฟล์: `src/EmployeeLeaveApi/Controllers/DeviceTokensController.cs`

```csharp
var existing = await _context.DeviceTokens.Find(filter).FirstOrDefaultAsync();
await _context.DeviceTokens.InsertOneAsync(deviceToken);
await _context.DeviceTokens.UpdateOneAsync(filter, update);
```

ใช้ทำอะไร:
- ลงทะเบียน token มือถือ
- ถ้ามีอยู่แล้วจะอัปเดตเวลาใช้งานล่าสุด

### 3.5 ตรวจ DB และ Seed ข้อมูลเริ่มต้น
ไฟล์: `src/EmployeeLeaveApi/Controllers/DatabaseCheckController.cs`

```csharp
var collections = await database.ListCollectionNames().ToListAsync();
var count = await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(new BsonDocument());
var users = await database.GetCollection<BsonDocument>("users").Find(new BsonDocument()).Limit(10).ToListAsync();
```

ใช้ทำอะไร:
- สำรวจรายชื่อ collection
- นับจำนวนเอกสารในแต่ละ collection
- ตรวจตัวอย่างข้อมูลใน collection `users`

```csharp
await _context.Roles.InsertManyAsync(roles);
await _context.Departments.InsertManyAsync(departments);
await _context.LeaveTypes.InsertManyAsync(leaveTypes);
var adminRole = await _context.Roles.Find(r => r.RoleName == "Admin").FirstOrDefaultAsync();
```

ใช้ทำอะไร:
- seed ข้อมูลพื้นฐาน (roles/departments/leave types)
- อ่าน role ที่ seed แล้วเพื่อนำไปสร้างข้อมูลอื่นต่อ

---

## 4) หมายเหตุฝั่ง Frontend

- ไม่พบการ query NoSQL โดยตรงในโค้ด frontend
- ที่พบคำว่า `find(...)` ในไฟล์ JavaScript เป็นการค้นหาใน array บนหน่วยความจำของแอป ไม่ใช่ query ไปฐานข้อมูล

---

## 5) สรุปสั้น

- โปรเจกต์นี้ใช้ MongoDB เป็น NoSQL หลักผ่าน `IMongoDbContext`
- Query หลักอยู่ใน controller/service ฝั่ง backend
- รูปแบบ query ที่ใช้จริง: `Find`, `FindAsync`, `CountDocumentsAsync`, `InsertOneAsync`, `InsertManyAsync`, `UpdateOneAsync`, `DeleteOneAsync`, `ReplaceOneAsync`
