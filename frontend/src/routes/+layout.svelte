<script lang="ts">
  import "./layout.css";
  import favicon_apple from "$lib/assets/favicons/apple-touch-icon.png";
  import favicon_ico from "$lib/assets/favicons/favicon.ico";
  import favicon_svg from "$lib/assets/favicons/favicon.svg";
  import favicon96 from "$lib/assets/favicons/favicon-96x96.png";

  import { ModeWatcher } from "mode-watcher";
  import { onDestroy, onMount } from "svelte";
  import { currentUserInformation } from "$lib/shared/shared.svelte";
  import type { Unsubscriber } from "svelte/store";
  import { setTheme } from "mode-watcher";
  let { children } = $props();

  let theme_watcher: Unsubscriber;
  onMount(() => {
    theme_watcher = currentUserInformation.subscribe((val) => {
      if (val.defaultTheme) {
        setTheme(val.defaultTheme);
      }
    });
  });
  onDestroy(() => {
    if (theme_watcher) {
      theme_watcher();
    }
  });
</script>

<svelte:head>
  <link rel="icon" type="image/png" href={favicon96} sizes="96x96" />
  <link rel="icon" type="image/svg+xml" href={favicon_svg} />
  <link rel="shortcut icon" href={favicon_ico} />
  <link rel="apple-touch-icon" sizes="180x180" href={favicon_apple} />
  <meta name="apple-mobile-web-app-title" content="Mods Together!" />
</svelte:head>
<ModeWatcher defaultTheme="cupcake"></ModeWatcher>
{@render children()}
