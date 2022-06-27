import React, { useState } from "react";

export type ImageFallbackProps = {
    src: string,
    fallbackSrc: string,
    [x: string]: any;
}

const ImageFallback = (props: ImageFallbackProps) => {
    const { src, fallbackSrc, ...rest } = props;

    return (
        // eslint-disable-next-line jsx-a11y/alt-text
        <img
            {...rest}
            src={src}
            onError={(e) => {
                e.currentTarget.onerror = null;
                e.currentTarget.src = fallbackSrc;
            }}
        />
    );
};

export default ImageFallback;
