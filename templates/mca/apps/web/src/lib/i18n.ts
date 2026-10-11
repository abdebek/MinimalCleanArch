export type Locale = "en" | "ar";

export const messages = {
  en: {
    app: "MCA",
    signIn: "Sign in",
    signOut: "Sign out",
    signUp: "Create account",
    todos: "Todos",
    email: "Email",
    password: "Password",
    title: "Title",
    add: "Add",
    forgot: "Forgot password",
    reset: "Reset password",
    confirm: "Confirm email",
    privacy: "Privacy",
    terms: "Terms",
    continueOidc: "Continue with OpenID Connect",
  },
  ar: {
    app: "MCA",
    signIn: "تسجيل الدخول",
    signOut: "تسجيل الخروج",
    signUp: "إنشاء حساب",
    todos: "المهام",
    email: "البريد",
    password: "كلمة المرور",
    title: "العنوان",
    add: "إضافة",
    forgot: "نسيت كلمة المرور",
    reset: "إعادة تعيين كلمة المرور",
    confirm: "تأكيد البريد",
    privacy: "الخصوصية",
    terms: "الشروط",
    continueOidc: "المتابعة عبر OpenID Connect",
  },
} as const;

const storageKey = "mca.locale";

export function readLocale(): Locale {
  if (typeof window === "undefined") return "en";
  const stored = window.localStorage.getItem(storageKey);
  return stored === "ar" ? "ar" : "en";
}

export function applyLocale(locale: Locale): void {
  document.documentElement.lang = locale;
  document.documentElement.dir = locale === "ar" ? "rtl" : "ltr";
  window.localStorage.setItem(storageKey, locale);
}

export function t(locale: Locale, key: keyof typeof messages.en): string {
  return messages[locale][key];
}
