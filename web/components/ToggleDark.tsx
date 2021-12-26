import {useTheme} from "next-themes";
import {useEffect, useState} from "react";

const ToggleDark = () => {
  const [mounted, setMounted] = useState(false);
  const { theme, setTheme } = useTheme();

  useEffect(() => setMounted(true), []);

  if (!mounted) return null;

  return (
    <>
      {theme === "dark"
        ? <i className="fas fa-sun fa-2x" onClick={() => setTheme("light")}/>
        : <i className="fas fa-moon fa-2x" onClick={() => setTheme("dark")}/>}
    </>
  );
};

export default ToggleDark;
