<script lang="ts">
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import ModTogetherHero from "$lib/components/LoginPage/ModTogetherHero.svelte";
</script>

<ModTogetherHero>
  <div>
    It's a lot like <a
      class="cursor-pointer underline"
      href="https://nusmods.com/">NUSMods</a
    >, but you can plan with your friends live!
  </div>

  <div>And your timetable carries over to every device 😊</div>

  <button class="btn my-5 btn-primary" onclick={() => goto(resolve("/login"))}
    >Try it out!</button
  >
</ModTogetherHero>
