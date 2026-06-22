import { AdminNavbar } from "../components/AdminNavbar";
import { Footer } from "../components/Footer";
import { AdminDashboardPage } from "../components/AdminDashboardPage";

export default function AdminDashboardPageWrapper() {
  return (
    <div className="min-h-screen flex flex-col">
      <AdminNavbar />
      <div className="flex-1">
        <AdminDashboardPage />
      </div>
      <Footer />
    </div>
  );
}
