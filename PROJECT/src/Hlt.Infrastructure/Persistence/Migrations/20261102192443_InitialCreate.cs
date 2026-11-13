using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hlt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_user",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    oidc_subject = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    role = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "department",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_department", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "priority_level",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    level_order = table.Column<short>(type: "smallint", nullable: false),
                    description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_priority_level", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patient",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    dob = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    insurance_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doctor",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    department_id = table.Column<int>(type: "int", nullable: false),
                    specialty = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    license_no = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doctor", x => x.id);
                    table.ForeignKey(
                        name: "fk_doctor_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_doctor_department_department_id",
                        column: x => x.department_id,
                        principalTable: "department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    department_id = table.Column<int>(type: "int", nullable: false),
                    position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    user_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_staff_department_department_id",
                        column: x => x.department_id,
                        principalTable: "department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doctor_schedule",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    doctor_id = table.Column<int>(type: "int", nullable: false),
                    work_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    slot_minutes = table.Column<short>(type: "smallint", nullable: false),
                    max_patients = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doctor_schedule", x => x.id);
                    table.CheckConstraint("ck_doctor_schedule_time", "[end_time] > [start_time]");
                    table.ForeignKey(
                        name: "fk_doctor_schedule_doctor_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "time_slot",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    schedule_id = table.Column<int>(type: "int", nullable: false),
                    start_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    end_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_slot", x => x.id);
                    table.ForeignKey(
                        name: "fk_time_slot_doctor_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "doctor_schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "appointment",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    patient_id = table.Column<int>(type: "int", nullable: false),
                    doctor_id = table.Column<int>(type: "int", nullable: false),
                    slot_id = table.Column<int>(type: "int", nullable: false),
                    priority_id = table.Column<int>(type: "int", nullable: false),
                    confirmed_by = table.Column<int>(type: "int", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointment", x => x.id);
                    table.ForeignKey(
                        name: "fk_appointment_doctor_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "doctor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointment_patient_patient_id",
                        column: x => x.patient_id,
                        principalTable: "patient",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointment_priority_level_priority_id",
                        column: x => x.priority_id,
                        principalTable: "priority_level",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointment_staff_confirmed_by",
                        column: x => x.confirmed_by,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointment_time_slot_slot_id",
                        column: x => x.slot_id,
                        principalTable: "time_slot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "appointment_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    appointment_id = table.Column<int>(type: "int", nullable: false),
                    changed_by = table.Column<int>(type: "int", nullable: false),
                    change_type = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    old_status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    new_status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointment_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_appointment_history_app_user_changed_by",
                        column: x => x.changed_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_appointment_history_appointment_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    appointment_id = table.Column<int>(type: "int", nullable: true),
                    channel = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_appointment_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "queue_ticket",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    appointment_id = table.Column<int>(type: "int", nullable: false),
                    department_id = table.Column<int>(type: "int", nullable: false),
                    priority_id = table.Column<int>(type: "int", nullable: false),
                    queue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    ticket_no = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    enqueued_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    called_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_queue_ticket", x => x.id);
                    table.ForeignKey(
                        name: "fk_queue_ticket_appointment_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_queue_ticket_department_department_id",
                        column: x => x.department_id,
                        principalTable: "department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_queue_ticket_priority_level_priority_id",
                        column: x => x.priority_id,
                        principalTable: "priority_level",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_user_email",
                table: "app_user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_user_oidc_subject",
                table: "app_user",
                column: "oidc_subject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointment_confirmed_by",
                table: "appointment",
                column: "confirmed_by");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_doctor_id",
                table: "appointment",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_patient_id",
                table: "appointment",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_priority_id",
                table: "appointment",
                column: "priority_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_status",
                table: "appointment",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_appointment_slot_active",
                table: "appointment",
                column: "slot_id",
                unique: true,
                filter: "[status] IN ('PENDING','CONFIRMED','CHECKED_IN','IN_PROGRESS')");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_history_appointment_id",
                table: "appointment_history",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_history_changed_by",
                table: "appointment_history",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "ix_department_name",
                table: "department",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_doctor_department_id",
                table: "doctor",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_license_no",
                table: "doctor",
                column: "license_no",
                unique: true,
                filter: "[license_no] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_user_id",
                table: "doctor",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_doctor_schedule_doctor_id_work_date_start_time",
                table: "doctor_schedule",
                columns: new[] { "doctor_id", "work_date", "start_time" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_appointment_id",
                table: "notification",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_status",
                table: "notification",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_notification_user_id",
                table: "notification",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_user_id",
                table: "patient",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_priority_level_code",
                table: "priority_level",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_queue_ticket_appointment_id",
                table: "queue_ticket",
                column: "appointment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_queue_ticket_department_id_queue_date_status",
                table: "queue_ticket",
                columns: new[] { "department_id", "queue_date", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_queue_ticket_department_id_queue_date_ticket_no",
                table: "queue_ticket",
                columns: new[] { "department_id", "queue_date", "ticket_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_queue_ticket_priority_id",
                table: "queue_ticket",
                column: "priority_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_department_id",
                table: "staff",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_staff_user_id",
                table: "staff",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_time_slot_schedule_id_start_at",
                table: "time_slot",
                columns: new[] { "schedule_id", "start_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_time_slot_schedule_id_status_start_at",
                table: "time_slot",
                columns: new[] { "schedule_id", "status", "start_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointment_history");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "queue_ticket");

            migrationBuilder.DropTable(
                name: "appointment");

            migrationBuilder.DropTable(
                name: "patient");

            migrationBuilder.DropTable(
                name: "priority_level");

            migrationBuilder.DropTable(
                name: "staff");

            migrationBuilder.DropTable(
                name: "time_slot");

            migrationBuilder.DropTable(
                name: "doctor_schedule");

            migrationBuilder.DropTable(
                name: "doctor");

            migrationBuilder.DropTable(
                name: "app_user");

            migrationBuilder.DropTable(
                name: "department");
        }
    }
}
