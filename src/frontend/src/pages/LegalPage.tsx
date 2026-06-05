import { Check, FileText, X } from "lucide-react";
import { useNavigate } from "react-router-dom";
import SystemPageShell from "./SystemPageShell";

const LEGAL_CONSENT_KEY = "reuse:legal-consent";

export default function LegalPage() {
  const navigate = useNavigate();

  const agree = () => {
    localStorage.setItem(LEGAL_CONSENT_KEY, new Date().toISOString());
    navigate("/signup");
  };

  return (
    <SystemPageShell
      eyebrow="Terms & Privacy"
      title="Terms and privacy policy"
      description="Review the basic terms for using ReUse and how the marketplace handles user data."
      icon={<FileText className="h-7 w-7" />}
      actions={[
        { label: "Agree", onClick: agree, icon: <Check className="h-4 w-4" /> },
        {
          label: "Decline",
          onClick: () => navigate("/"),
          variant: "secondary",
          icon: <X className="h-4 w-4" />,
        },
      ]}
    >
      <section>
        <h2 className="text-lg font-semibold text-gray-950">Terms of use</h2>
        <p className="mt-2">
          Use ReUse to browse, list, sell, request, and swap items honestly. Listings should be
          accurate, lawful, and safe for other users to evaluate.
        </p>
      </section>
      <section>
        <h2 className="text-lg font-semibold text-gray-950">User responsibility</h2>
        <p className="mt-2">
          You are responsible for the information you submit, your account activity, and your
          communication with other users.
        </p>
      </section>
      <section>
        <h2 className="text-lg font-semibold text-gray-950">Privacy policy</h2>
        <p className="mt-2">
          ReUse uses account, profile, listing, browsing, and communication data to provide the
          marketplace experience.
        </p>
      </section>
      <section>
        <h2 className="text-lg font-semibold text-gray-950">How data supports ReUse</h2>
        <p className="mt-2">
          Data supports authentication, product discovery, recommendations, notifications,
          moderation, and customer support.
        </p>
      </section>
      <section>
        <h2 className="text-lg font-semibold text-gray-950">Your choices</h2>
        <p className="mt-2">
          You can decline and return home. Future account controls can expand how users review or
          manage their data.
        </p>
      </section>
    </SystemPageShell>
  );
}
