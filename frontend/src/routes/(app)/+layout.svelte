<script lang="ts">
  import { HouseHeart } from "@lucide/svelte";
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import mods_tgt_header from "$lib/assets/mods_tgt_header.png?enhanced";
  import type { LayoutProps } from "./$types";
  import NavigationBar from "./NavigationBar.svelte";

  let { data, children, params }: LayoutProps = $props();
</script>

<NavigationBar {children} {data} {params}></NavigationBar>
