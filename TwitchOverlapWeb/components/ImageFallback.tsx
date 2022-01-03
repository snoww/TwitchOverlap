import React, { useState } from "react";

export type ImageFallbackProps = {
    src: string,
    fallbackSrc: string,
    [x: string]: any;
}

const ImageFallback = (props: ImageFallbackProps) => {
    const { src, fallbackSrc, ...rest } = props;
    const [imgSrc, setImgSrc] = useState(false);
    const [oldSrc, setOldSrc] = useState(src);
    if (oldSrc !== src) {
        setImgSrc(false);
        setOldSrc(src);
    }
    return (
        // eslint-disable-next-line jsx-a11y/alt-text
        <img
            {...rest}
            src={imgSrc ? fallbackSrc : src}
            onError={() => {
                setImgSrc(true);
            }}
        />
    );
};

export default ImageFallback;