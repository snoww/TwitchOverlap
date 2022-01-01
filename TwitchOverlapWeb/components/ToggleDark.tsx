import {useTheme} from "next-themes";
import React, {ReactNode, useEffect, useState} from "react";

type Props = {
  children?: ReactNode
}

const ToggleDark = ({children} : Props) => {
  const [mounted, setMounted] = useState(false);
  const { theme, setTheme } = useTheme();

  useEffect(() => setMounted(true), []);

  if (!mounted) return null;

  return (
    <div className="flex items-center" onClick={() => setTheme(theme === "dark" ? "light" : "dark" )}>
      {theme === "dark"
        ? <i className="fas fa-sun fa-2x"/>
        : <i className="fas fa-moon fa-2x"/>}
      {children}
    </div>
  );
};

export default ToggleDark;
