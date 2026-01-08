// /Web/data/mock-data.js
const employeeDashboardData = {
    user: {
        name: "สมชาย ใจดี",
        role: "พนักงาน",
        department: "IT Support"
    },
    summary: {
        totalRequest: 2,
        pendingApproval: 1,
        totalDayOff: 1
    },
    leaveBalances: [
        { type: "Annual Leave", used: 2, total: 6, color: "bg-blue-400", icon: "sun" }, // สิทธิลาพักผ่อน 6 วัน [cite: 59]
        { type: "Sick Leave", used: 1, total: 30, color: "bg-red-400", icon: "heart" }, // สิทธิลาป่วย 30 วัน [cite: 52]
        { type: "Personal Leave", used: 0, total: 3, color: "bg-purple-400", icon: "user" }, // สิทธิลากิจ 3 วัน [cite: 55]
        { type: "Ordination Leave", used: 0, total: 15, color: "bg-indigo-400", icon: "hands" }, // ลาอุปสมบท [cite: 48, 66]
        { type: "Unpaid Leave", used: 0, total: 365, color: "bg-gray-400", icon: "dollar-sign" }
    ],
    recentRequests: [
        { type: "Sick Leave", start: "Jan 15 2025", end: "Jan 19 2025", days: 5, status: "Approved", reason: "Covid-19" },
        { type: "Unpaid Leave", start: "Jan 20 2025", end: "Jan 21 2025", days: 1, status: "Pending", reason: "ธุระส่วนตัว" }
    ]
};