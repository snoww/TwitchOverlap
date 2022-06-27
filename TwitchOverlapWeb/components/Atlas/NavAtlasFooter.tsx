import NavAtlasSelectMonth from "./NavAtlasSelectMonth";

const NavAtlasFooter = ({name, index}: {name: string, index: number}) => {
  return (
    <>
      <div className="absolute bottom-0 right-0 flex flex-col m-2">
        <NavAtlasSelectMonth index={index}/>
        <div className="font-mono text-sm ml-auto">stats.roki.sh/atlas</div>
      </div>
    </>
  );
};

export default NavAtlasFooter;
