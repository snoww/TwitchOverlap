import Document, {DocumentContext, Head, Html, Main, NextScript} from "next/document";
import React from "react";

class MyDocument extends Document {
  static async getInitialProps(ctx: DocumentContext) {
    return await Document.getInitialProps(ctx);
  }

  render() {
    return (
      <Html lang="en" className={"dark"}>
        <Head>
          {/* Global site tag (gtag.js) - Google Analytics*/}
          {/*<script*/}
          {/*  async*/}
          {/*  src="https://www.googletagmanager.com/gtag/js?id=G-53NJ04WTT9"*/}
          {/*/>*/}
          {/*<script*/}
          {/*  dangerouslySetInnerHTML={{*/}
          {/*    __html: `*/}
          {/*    window.dataLayer = window.dataLayer || [];*/}
          {/*    function gtag(){dataLayer.push(arguments);}*/}
          {/*    gtag('js', new Date());*/}
          {/*  */}
          {/*    gtag('config', 'G-53NJ04WTT9');`*/}
          {/*  }}*/}
          {/*/>*/}
          <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.2/css/all.min.css"
                integrity="sha512-HK5fgLBL+xu6dm/Ii3z4xhlSUyZgTT9tuc/hSrtw6uzJOvgRr2a9jyxxT1ely+B+xFAmJKVSTbpM/CuL7qxO8w=="
                crossOrigin="anonymous"/>
          <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800&display=swap"
                rel="stylesheet"/>
        </Head>
        <body className={"bg-white dark:bg-gray-800 dark:text-white"}>
        <Main/>
        <NextScript/>
        </body>
      </Html>
    );
  }
}

export default MyDocument;
